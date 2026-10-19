import React, { useState } from 'react';

const MESSAGE_COLORS = {
  success: { bg: '#10B981', icon: '✅' },
  warning: { bg: '#F59E0B', icon: '⚠️' },
  error: { bg: '#EF4444', icon: '❌' },
  info: { bg: '#3B82F6', icon: 'ℹ️' }
};

// ---------- טופס התחברות / הרשמה ----------
function AuthPanel({ onAuth, authError, busy }) {
  const [mode, setMode] = useState('login');
  const [form, setForm] = useState({ email: '', password: '', firstName: '', lastName: '' });
  const update = (field) => (e) => setForm({ ...form, [field]: e.target.value });

  const submit = (e) => {
    e.preventDefault();
    const body = mode === 'login'
      ? { email: form.email, password: form.password }
      : form;
    onAuth(mode, body);
  };

  return (
    <form onSubmit={submit} style={styles.authPanel}>
      <div style={styles.tabs}>
        {['login', 'register'].map(m => (
          <button key={m} type="button" onClick={() => setMode(m)}
            style={{ ...styles.tabButton, ...(mode === m ? styles.activeTab : {}) }}>
            {m === 'login' ? 'התחברות' : 'הרשמה'}
          </button>
        ))}
      </div>

      {mode === 'register' && (
        <div style={styles.row}>
          <input style={styles.input} placeholder="שם פרטי" value={form.firstName} onChange={update('firstName')} required />
          <input style={styles.input} placeholder="שם משפחה" value={form.lastName} onChange={update('lastName')} required />
        </div>
      )}
      <div style={styles.row}>
        <input style={styles.input} type="email" placeholder="אימייל" value={form.email} onChange={update('email')} required />
        <input style={styles.input} type="password" placeholder="סיסמה (6 תווים לפחות)" value={form.password} onChange={update('password')} required minLength={mode === 'register' ? 6 : undefined} />
        <button type="submit" disabled={busy} style={styles.submitBtn}>
          {mode === 'login' ? 'התחבר' : 'הירשם'}
        </button>
      </div>
      {authError && <div style={styles.authError}>{authError}</div>}
      <div style={styles.hint}>
        משתמשי דמו: user@example.com · user2@example.com · admin@example.com — סיסמה: 123456
      </div>
    </form>
  );
}

// ---------- כרטיס פריט ----------
function ItemCard({ item, isMine, busy, onLoan, onReturn }) {
  const available = item.availableCopies > 0;
  const badge = isMine
    ? { text: 'מושאל אצלך', color: '#6366F1' }
    : available
      ? { text: 'זמין להשאלה', color: '#10B981' }
      : { text: 'מושאל', color: '#F59E0B' };

  return (
    <div style={styles.itemCard}>
      <div style={styles.cardHeader}>
        <span style={{ ...styles.statusBadge, backgroundColor: badge.color }}>{badge.text}</span>
        <span style={styles.copyId}>זמינים: {item.availableCopies}/{item.totalCopies}</span>
      </div>
      <h3 style={styles.itemName}>{item.name}</h3>
      <p style={styles.itemDescription}>{item.description || 'ללא תיאור'}</p>
      {item.categories?.length > 0 && (
        <div style={styles.categories}>
          {item.categories.map(c => <span key={c} style={styles.categoryTag}>{c}</span>)}
        </div>
      )}
      <div style={styles.cardFooter}>
        {/* הכפתור לא מושבת גם כשהפריט מושאל - כך לחיצה נוספת מציגה "זה כבר מושאל" */}
        <button onClick={() => onLoan(item)} disabled={busy} style={styles.loanBtn}>השאל עכשיו</button>
        {isMine && (
          <button onClick={() => onReturn(item)} disabled={busy} style={styles.returnBtn}>החזר</button>
        )}
      </div>
    </div>
  );
}

// ---------- מסך ראשי ----------
export default function Dashboard({
  user, offline, busy, items, totalCount, page, totalPages, onPageChange,
  search, onSearchChange, message, onCloseMessage, myItemIds, activeLoansCount,
  onLoan, onReturn, onAuth, authError, onLogout, onRetryServer, onResetDemo
}) {
  const availableOnPage = items.filter(i => i.availableCopies > 0).length;
  const stats = [
    { title: 'סה"כ פריטים', value: totalCount, icon: '📦', gradient: 'linear-gradient(135deg, #FF6B6B, #FF8E53)' },
    { title: 'זמינים בעמוד זה', value: availableOnPage, icon: '✅', gradient: 'linear-gradient(135deg, #11998e, #38ef7d)' },
    { title: 'ההשאלות שלי', value: activeLoansCount, icon: '🔄', gradient: 'linear-gradient(135deg, #4E65FF, #92EFFD)' }
  ];
  const msgStyle = message ? MESSAGE_COLORS[message.type] || MESSAGE_COLORS.info : null;

  return (
    <div style={styles.container}>
      <header style={styles.header}>
        <div>
          <h1 style={styles.title}>מערכת השאלת ציוד</h1>
          <p style={styles.subtitle}>
            {user ? `מחובר/ת: ${user.fullName || user.email} (${user.role === 'Admin' ? 'מנהל' : 'משתמש'})` : 'בחרו פריט ולחצו "השאל עכשיו"'}
          </p>
        </div>
        {user && !offline && (
          <button onClick={onLogout} style={{ ...styles.primaryBtn, backgroundColor: '#EF4444', color: '#FFF' }}>התנתק</button>
        )}
      </header>

      {offline && (
        <div style={styles.offlineBanner}>
          <span>מצב הדגמה: השרת לא זמין, הציוד נטען מהרשימה המוגדרת בקובץ. ההשאלות נשמרות בדפדפן זה בלבד.</span>
          <span style={{ display: 'flex', gap: 8 }}>
            <button onClick={onRetryServer} style={styles.smallBtn}>נסה להתחבר לשרת</button>
            <button onClick={onResetDemo} style={styles.smallBtn}>איפוס הדגמה</button>
          </span>
        </div>
      )}

      {message && (
        <div role="alert" style={{ ...styles.messageBanner, backgroundColor: msgStyle.bg }} onClick={onCloseMessage}>
          {msgStyle.icon} {message.text}
        </div>
      )}

      {!user && !offline && <AuthPanel onAuth={onAuth} authError={authError} busy={busy} />}

      <section style={styles.statsGrid}>
        {stats.map(stat => (
          <div key={stat.title} style={{ ...styles.statCard, background: stat.gradient }}>
            <div style={styles.statIcon}>{stat.icon}</div>
            <div>
              <div style={styles.statValue}>{stat.value}</div>
              <div style={styles.statTitle}>{stat.title}</div>
            </div>
          </div>
        ))}
      </section>

      <main style={styles.mainContent}>
        <div style={styles.filterBar}>
          <h2 style={{ margin: 0, color: '#1F2937' }}>הציוד</h2>
          <input
            type="text"
            placeholder="חיפוש לפי שם או תיאור..."
            value={search}
            onChange={(e) => onSearchChange(e.target.value)}
            style={styles.searchInput}
          />
        </div>

        <div style={styles.itemsGrid}>
          {items.length === 0 ? (
            <p style={{ color: '#6B7280' }}>לא נמצא ציוד.</p>
          ) : (
            items.map(item => (
              <ItemCard key={item.id} item={item} isMine={myItemIds.has(item.id)}
                busy={busy} onLoan={onLoan} onReturn={onReturn} />
            ))
          )}
        </div>

        <div style={styles.pagination}>
          <button style={styles.pageBtn} disabled={page <= 1} onClick={() => onPageChange(page - 1)}>הקודם</button>
          <span>עמוד {page} מתוך {totalPages}</span>
          <button style={styles.pageBtn} disabled={page >= totalPages} onClick={() => onPageChange(page + 1)}>הבא</button>
        </div>
      </main>
    </div>
  );
}

const styles = {
  container: { fontFamily: 'Segoe UI, Tahoma, Geneva, Verdana, sans-serif', direction: 'rtl', backgroundColor: '#F3F4F6', minHeight: '100vh', padding: '24px', boxSizing: 'border-box' },
  header: { display: 'flex', flexWrap: 'wrap', gap: '16px', justifyContent: 'space-between', alignItems: 'center', background: 'linear-gradient(135deg, #6366F1 0%, #A855F7 100%)', padding: '24px 32px', borderRadius: '20px', color: '#FFFFFF', boxShadow: '0 10px 25px -5px rgba(99, 102, 241, 0.4)', marginBottom: '20px' },
  title: { margin: 0, fontSize: '28px', fontWeight: 800 },
  subtitle: { margin: '6px 0 0 0', opacity: 0.9, fontSize: '15px' },
  primaryBtn: { backgroundColor: '#FFFFFF', color: '#4F46E5', border: 'none', padding: '12px 24px', borderRadius: '12px', fontWeight: 700, fontSize: '15px', cursor: 'pointer' },
  offlineBanner: { display: 'flex', flexWrap: 'wrap', gap: '12px', justifyContent: 'space-between', alignItems: 'center', backgroundColor: '#FEF3C7', color: '#92400E', border: '1px solid #FCD34D', padding: '12px 18px', borderRadius: '12px', marginBottom: '16px', fontSize: '14px' },
  smallBtn: { border: '1px solid #D97706', background: '#FFF', color: '#92400E', borderRadius: '8px', padding: '6px 12px', cursor: 'pointer', fontWeight: 600 },
  messageBanner: { color: '#FFFFFF', padding: '16px 20px', borderRadius: '12px', marginBottom: '20px', fontWeight: 700, fontSize: '18px', textAlign: 'center', cursor: 'pointer', boxShadow: '0 4px 10px rgba(0,0,0,0.15)' },
  authPanel: { backgroundColor: '#FFFFFF', borderRadius: '16px', padding: '20px', marginBottom: '20px', display: 'flex', flexDirection: 'column', gap: '12px' },
  row: { display: 'flex', flexWrap: 'wrap', gap: '10px' },
  input: { flex: '1 1 180px', padding: '10px 14px', borderRadius: '10px', border: '1px solid #E5E7EB', fontSize: '14px' },
  submitBtn: { padding: '10px 24px', backgroundColor: '#4F46E5', color: '#FFF', border: 'none', borderRadius: '10px', fontWeight: 700, cursor: 'pointer' },
  authError: { color: '#B91C1C', fontWeight: 600 },
  hint: { color: '#6B7280', fontSize: '13px' },
  tabs: { display: 'flex', gap: '8px', backgroundColor: '#F3F4F6', padding: '6px', borderRadius: '12px', alignSelf: 'flex-start' },
  tabButton: { padding: '8px 18px', border: 'none', borderRadius: '8px', backgroundColor: 'transparent', color: '#4B5563', fontWeight: 600, cursor: 'pointer' },
  activeTab: { backgroundColor: '#FFFFFF', color: '#4F46E5', boxShadow: '0 2px 8px rgba(0,0,0,0.08)' },
  statsGrid: { display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '20px', marginBottom: '24px' },
  statCard: { borderRadius: '16px', padding: '20px', color: '#FFFFFF', display: 'flex', alignItems: 'center', gap: '16px' },
  statIcon: { fontSize: '30px', backgroundColor: 'rgba(255, 255, 255, 0.25)', padding: '12px', borderRadius: '14px' },
  statValue: { fontSize: '26px', fontWeight: 800 },
  statTitle: { fontSize: '14px', opacity: 0.95 },
  mainContent: { backgroundColor: '#FFFFFF', borderRadius: '20px', padding: '24px' },
  filterBar: { display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px', flexWrap: 'wrap', gap: '16px' },
  searchInput: { padding: '10px 16px', borderRadius: '10px', border: '1px solid #E5E7EB', width: '280px', maxWidth: '100%', fontSize: '14px' },
  itemsGrid: { display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(250px, 1fr))', gap: '20px' },
  itemCard: { border: '1px solid #E5E7EB', borderRadius: '16px', padding: '20px', backgroundColor: '#FAFAFA', display: 'flex', flexDirection: 'column', justifyContent: 'space-between' },
  cardHeader: { display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '12px' },
  statusBadge: { color: '#FFFFFF', padding: '4px 10px', borderRadius: '20px', fontSize: '12px', fontWeight: 700 },
  copyId: { fontSize: '12px', color: '#6B7280' },
  itemName: { margin: '0 0 6px 0', fontSize: '18px', color: '#1F2937' },
  itemDescription: { margin: '0 0 10px 0', fontSize: '14px', color: '#6B7280' },
  categories: { display: 'flex', flexWrap: 'wrap', gap: '6px', marginBottom: '14px' },
  categoryTag: { backgroundColor: '#EEF2FF', color: '#4338CA', borderRadius: '6px', padding: '2px 8px', fontSize: '12px' },
  cardFooter: { display: 'flex', gap: '8px' },
  loanBtn: { flex: 1, padding: '10px 0', backgroundColor: '#4F46E5', color: '#FFFFFF', border: 'none', borderRadius: '10px', fontWeight: 700, cursor: 'pointer', fontSize: '14px' },
  returnBtn: { padding: '10px 16px', backgroundColor: '#FFFFFF', color: '#4F46E5', border: '1px solid #C7D2FE', borderRadius: '10px', fontWeight: 700, cursor: 'pointer', fontSize: '14px' },
  pagination: { display: 'flex', justifyContent: 'center', alignItems: 'center', gap: '16px', marginTop: '24px', color: '#374151' },
  pageBtn: { padding: '8px 18px', borderRadius: '8px', border: '1px solid #E5E7EB', background: '#FFF', cursor: 'pointer', fontWeight: 600 }
};
