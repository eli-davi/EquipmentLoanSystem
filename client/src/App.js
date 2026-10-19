import React, { useCallback, useEffect, useMemo, useState } from 'react';
import Dashboard from './Dashboard';

// כתובת השרת. אפשר לשנות ב-.env:  REACT_APP_API_URL=http://localhost:5000
const API_URL = process.env.REACT_APP_API_URL || 'http://localhost:5000';
const PAGE_SIZE = 6;

export const MSG_LOAN_SUCCESS = 'הפריט הושאל בהצלחה';
export const MSG_ALREADY_LOANED = 'זה כבר מושאל';

/*
 * רשימת הציוד מוגדרת כאן בקובץ עצמו (זהה ל-Seed שבשרת - SeedData ב-AppDbContext).
 * כשהשרת זמין - הרשימה מגיעה ממנו (עם pagination אמיתי).
 * כשהשרת לא זמין (למשל מי שפותח רק את הלקוח דרך קישור) - המערכת עוברת ל"מצב הדגמה"
 * ועובדת מול הרשימה הזו, כולל השאלה, החזרה וההודעות.
 */
const LOCAL_EQUIPMENT = [
  { id: 1, name: 'מצלמת Canon EOS R6', description: 'מצלמה מקצועית ללא מראה, כולל עדשת 24-105', categories: ['צילום'], totalCopies: 1, availableCopies: 1 },
  { id: 2, name: 'מקרן Epson Full HD', description: 'מקרן 3,500 לומן עם כבל HDMI', categories: ['הקרנה'], totalCopies: 1, availableCopies: 0 },
  { id: 3, name: 'ערכת סאונד ניידת JBL', description: 'רמקול נייד עם סוללה ל-12 שעות', categories: ['שמע'], totalCopies: 1, availableCopies: 1 },
  { id: 4, name: 'תאורת סטודיו Godox', description: 'זוג פנסי LED עם חצובות', categories: ['צילום', 'תאורה'], totalCopies: 1, availableCopies: 1 },
  { id: 5, name: 'מחשב נייד Dell Latitude', description: "מחשב נייד 14 אינץ' להצגות ולעבודה", categories: ['הקרנה', 'מחשוב'], totalCopies: 1, availableCopies: 1 },
  { id: 6, name: 'חצובה מקצועית Manfrotto', description: 'חצובת אלומיניום עם ראש נע', categories: ['צילום'], totalCopies: 1, availableCopies: 1 },
  { id: 7, name: 'מיקרופון אלחוטי Shure', description: 'מיקרופון יד אלחוטי עם מקלט', categories: ['שמע'], totalCopies: 1, availableCopies: 1 },
  { id: 8, name: "מסך הקרנה נייד 100 אינץ'", description: 'מסך על חצובה, מתקפל לתיק נשיאה', categories: ['הקרנה'], totalCopies: 1, availableCopies: 1 }
];

// ---------- localStorage עטוף ב-try/catch (יכול להיכשל בחלון פרטי וכו') ----------
const storage = {
  get(key, fallback) {
    try {
      const raw = window.localStorage.getItem(key);
      return raw ? JSON.parse(raw) : fallback;
    } catch {
      return fallback;
    }
  },
  set(key, value) {
    try {
      if (value === null || value === undefined) window.localStorage.removeItem(key);
      else window.localStorage.setItem(key, JSON.stringify(value));
    } catch {
      /* מתעלמים */
    }
  }
};

class NetworkError extends Error {}

async function apiFetch(path, { method = 'GET', body, token } = {}) {
  const headers = { 'Content-Type': 'application/json' };
  if (token) headers.Authorization = `Bearer ${token}`; // ה-JWT נשלח ב-header

  let response;
  try {
    response = await fetch(`${API_URL}${path}`, {
      method,
      headers,
      body: body ? JSON.stringify(body) : undefined
    });
  } catch {
    throw new NetworkError('השרת לא זמין');
  }

  let data = null;
  const text = await response.text();
  if (text) {
    try {
      data = JSON.parse(text);
    } catch {
      data = { message: text };
    }
  }
  return { status: response.status, ok: response.ok, data };
}

// הודעת שגיאה קריאה מתשובת השרת (כולל ValidationProblem של ASP.NET)
function errorText(data, fallback) {
  if (!data) return fallback;
  if (data.message) return data.message;
  if (data.errors) return Object.values(data.errors).flat().join(' · ');
  return data.title || fallback;
}

export default function App() {
  const [auth, setAuth] = useState(() => storage.get('auth', null));
  const [offline, setOffline] = useState(false);

  // נתונים מהשרת
  const [items, setItems] = useState([]);
  const [totalCount, setTotalCount] = useState(0);
  const [myLoans, setMyLoans] = useState([]);

  // נתונים במצב הדגמה (מקומי)
  const [localItems, setLocalItems] = useState(() => storage.get('localItems', LOCAL_EQUIPMENT));
  const [localMine, setLocalMine] = useState(() => storage.get('localMine', []));

  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [message, setMessage] = useState(null); // { text, type: 'success' | 'warning' | 'error' | 'info' }
  const [authError, setAuthError] = useState('');
  const [busy, setBusy] = useState(false);

  const token = auth?.token;
  const showMessage = useCallback((text, type = 'info') => setMessage({ text, type, at: Date.now() }), []);

  useEffect(() => storage.set('auth', auth), [auth]);
  useEffect(() => storage.set('localItems', localItems), [localItems]);
  useEffect(() => storage.set('localMine', localMine), [localMine]);

  // ההודעה נעלמת אחרי 4 שניות
  useEffect(() => {
    if (!message) return undefined;
    const timer = setTimeout(() => setMessage(null), 4000);
    return () => clearTimeout(timer);
  }, [message]);

  const logout = useCallback((text) => {
    setAuth(null);
    setMyLoans([]);
    if (text) showMessage(text, 'info');
  }, [showMessage]);

  // ---------- טעינת רשימה עם pagination אמיתי מול השרת ----------
  const loadItems = useCallback(async () => {
    try {
      const params = new URLSearchParams({ page, pageSize: PAGE_SIZE, sortBy: 'id' });
      if (search.trim()) params.set('search', search.trim());
      const res = await apiFetch(`/api/items?${params}`);
      if (!res.ok) {
        showMessage(errorText(res.data, 'שגיאה בטעינת הציוד'), 'error');
        return;
      }
      setOffline(false);
      setItems(res.data.items || []);
      setTotalCount(res.data.totalCount || 0);
    } catch (e) {
      if (e instanceof NetworkError) setOffline(true);
    }
  }, [page, search, showMessage]);

  const loadMyLoans = useCallback(async () => {
    if (!token || offline) return;
    try {
      const res = await apiFetch('/api/loans/mine?activeOnly=true', { token });
      if (res.status === 401) return logout('פג תוקף ההתחברות, יש להתחבר מחדש');
      if (res.ok) setMyLoans(res.data || []);
    } catch {
      /* השרת לא זמין - יטופל ב-loadItems */
    }
  }, [token, offline, logout]);

  useEffect(() => { loadItems(); }, [loadItems]);
  useEffect(() => { loadMyLoans(); }, [loadMyLoans]);

  // ---------- מצב הדגמה: סינון ו-pagination על הרשימה המקומית ----------
  const localView = useMemo(() => {
    const term = search.trim().toLowerCase();
    const filtered = localItems.filter(i =>
      !term || i.name.toLowerCase().includes(term) || i.description.toLowerCase().includes(term));
    return {
      total: filtered.length,
      items: filtered.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE)
    };
  }, [localItems, search, page]);

  const shownItems = offline ? localView.items : items;
  const shownTotal = offline ? localView.total : totalCount;
  const totalPages = Math.max(1, Math.ceil(shownTotal / PAGE_SIZE));
  const myItemIds = useMemo(
    () => new Set(offline ? localMine : myLoans.map(l => l.itemId)),
    [offline, localMine, myLoans]
  );

  // ---------- השאלה ----------
  const handleLoan = async (item) => {
    if (offline) {
      const current = localItems.find(i => i.id === item.id);
      if (!current || current.availableCopies <= 0) {
        showMessage(MSG_ALREADY_LOANED, 'warning');
        return;
      }
      setLocalItems(prev => prev.map(i => (i.id === item.id ? { ...i, availableCopies: i.availableCopies - 1 } : i)));
      setLocalMine(prev => [...prev, item.id]);
      showMessage(MSG_LOAN_SUCCESS, 'success');
      return;
    }

    if (!token) {
      showMessage('יש להתחבר כדי להשאיל ציוד', 'info');
      return;
    }

    setBusy(true);
    try {
      const res = await apiFetch('/api/loans', { method: 'POST', body: { itemId: item.id }, token });

      if (res.status === 201) {
        showMessage(MSG_LOAN_SUCCESS, 'success');
      } else if (res.status === 409) {
        // אין עותק פנוי, או שמשתמש אחר תפס אותו באותו רגע (DbUpdateConcurrencyException בשרת)
        showMessage(MSG_ALREADY_LOANED, 'warning');
      } else if (res.status === 401) {
        logout('פג תוקף ההתחברות, יש להתחבר מחדש');
      } else {
        showMessage(errorText(res.data, 'ההשאלה נכשלה'), 'error');
      }
    } catch (e) {
      if (e instanceof NetworkError) {
        setOffline(true);
        showMessage('השרת לא זמין - עברנו למצב הדגמה', 'error');
      }
    } finally {
      setBusy(false);
      await Promise.all([loadItems(), loadMyLoans()]);
    }
  };

  // ---------- החזרה ----------
  const handleReturn = async (item) => {
    if (offline) {
      setLocalItems(prev => prev.map(i => (i.id === item.id ? { ...i, availableCopies: Math.min(i.totalCopies, i.availableCopies + 1) } : i)));
      setLocalMine(prev => {
        const idx = prev.indexOf(item.id);
        return idx === -1 ? prev : [...prev.slice(0, idx), ...prev.slice(idx + 1)];
      });
      showMessage('הפריט הוחזר בהצלחה', 'success');
      return;
    }

    const loan = myLoans.find(l => l.itemId === item.id);
    if (!loan) return;

    setBusy(true);
    try {
      const res = await apiFetch(`/api/loans/${loan.id}`, { method: 'PATCH', body: { status: 'Returned' }, token });
      if (res.status === 204) showMessage('הפריט הוחזר בהצלחה', 'success');
      else if (res.status === 401) logout('פג תוקף ההתחברות, יש להתחבר מחדש');
      else showMessage(errorText(res.data, 'ההחזרה נכשלה'), res.status === 409 ? 'warning' : 'error');
    } catch {
      setOffline(true);
    } finally {
      setBusy(false);
      await Promise.all([loadItems(), loadMyLoans()]);
    }
  };

  // ---------- התחברות / הרשמה ----------
  const handleAuth = async (mode, form) => {
    setAuthError('');
    setBusy(true);
    try {
      const path = mode === 'register' ? '/api/auth/register' : '/api/auth/login';
      const res = await apiFetch(path, { method: 'POST', body: form });
      if (res.ok) {
        setAuth(res.data); // { token, email, fullName, role, ... }
        showMessage(`שלום ${res.data.fullName || res.data.email}`, 'success');
      } else {
        setAuthError(errorText(res.data, 'הפעולה נכשלה'));
      }
    } catch {
      setOffline(true);
      setAuthError('השרת לא זמין');
    } finally {
      setBusy(false);
    }
  };

  const resetDemo = () => {
    setLocalItems(LOCAL_EQUIPMENT);
    setLocalMine([]);
    showMessage('נתוני ההדגמה אופסו', 'info');
  };

  return (
    <Dashboard
      user={auth}
      offline={offline}
      busy={busy}
      items={shownItems}
      totalCount={shownTotal}
      page={page}
      totalPages={totalPages}
      onPageChange={setPage}
      search={search}
      onSearchChange={(value) => { setSearch(value); setPage(1); }}
      message={message}
      onCloseMessage={() => setMessage(null)}
      myItemIds={myItemIds}
      activeLoansCount={offline ? localMine.length : myLoans.length}
      onLoan={handleLoan}
      onReturn={handleReturn}
      onAuth={handleAuth}
      authError={authError}
      onLogout={() => logout('התנתקת מהמערכת')}
      onRetryServer={loadItems}
      onResetDemo={resetDemo}
    />
  );
}
