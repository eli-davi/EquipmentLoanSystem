import React, { useState } from 'react';

const Dashboard = () => {
  const [activeTab, setActiveTab] = useState('all');

  const stats = [
    { title: 'סה"כ ציוד', value: '124', icon: '📦', gradient: 'linear-gradient(135deg, #FF6B6B, #FF8E53)' },
    { title: 'מושלים כעת', value: '42', icon: '🔄', gradient: 'linear-gradient(135deg, #4E65FF, #92EFFD)' },
    { title: 'זמינים למסירה', value: '78', icon: '✅', gradient: 'linear-gradient(135deg, #11998e, #38ef7d)' },
    { title: 'התראות/איחורים', value: '4', icon: '⚠️', gradient: 'linear-gradient(135deg, #FF416C, #FF4B2B)' }
  ];

  const items = [
    { id: 1, name: 'מצלמת Canon EOS R6', category: 'צילום', status: 'זמין', copyId: 'SN-001', tagColor: '#10B981' },
    { id: 2, name: 'מקרן Epson Full HD', category: 'הקרנה', status: 'מוגאל', copyId: 'SN-002', tagColor: '#F59E0B' },
    { id: 3, name: 'ערכת סאונד ניידת JBL', category: 'שמע', status: 'זמין', copyId: 'SN-003', tagColor: '#10B981' },
    { id: 4, name: 'תאורת סטודיו Godox', category: 'תאורה', status: 'בתחזוקה', copyId: 'SN-004', tagColor: '#EF4444' }
  ];

  return (
    <div style={styles.container}>
      {/* סרגל עליון צבעוני */}
      <header style={styles.header}>
        <div>
          <h1 style={styles.title}>מערכת ניהול ציוד מתקדמת</h1>
          <p style={styles.subtitle}>ברוכים הבאים לפאנל השליטה וההשאלות</p>
        </div>
        <button style={styles.primaryBtn}>+ הוספת פריט חדש</button>
      </header>

      {/* כרטיסי סטטיסטיקה */}
      <section style={styles.statsGrid}>
        {stats.map((stat, index) => (
          <div key={index} style={{ ...styles.statCard, background: stat.gradient }}>
            <div style={styles.statIcon}>{stat.icon}</div>
            <div>
              <div style={styles.statValue}>{stat.value}</div>
              <div style={styles.statTitle}>{stat.title}</div>
            </div>
          </div>
        ))}
      </section>

      {/* אזור תוכן מרכזי */}
      <main style={styles.mainContent}>
        <div style={styles.filterBar}>
          <div style={styles.tabs}>
            {['all', 'available', 'loaned'].map((tab) => (
              <button
                key={tab}
                onClick={() => setActiveTab(tab)}
                style={{
                  ...styles.tabButton,
                  ...(activeTab === tab ? styles.activeTab : {})
                }}
              >
                {tab === 'all' ? 'כל הציוד' : tab === 'available' ? 'זמינים' : 'מואלים'}
              </button>
            ))}
          </div>
          <input type="text" placeholder="חיפוש ציוד לפי שם או מספר סידורי..." style={styles.searchInput} />
        </div>

        {/* גריד כרטיסי פריטים */}
        <div style={styles.itemsGrid}>
          {items.map((item) => (
            <div key={item.id} style={styles.itemCard}>
              <div style={styles.cardHeader}>
                <span style={{ ...styles.statusBadge, backgroundColor: item.tagColor }}>
                  {item.status}
                </span>
                <span style={styles.copyId}>מזהה עותק: {item.copyId}</span>
              </div>
              <h3 style={styles.itemName}>{item.name}</h3>
              <p style={styles.itemCategory}>קטגוריה: {item.category}</p>
              <div style={styles.cardFooter}>
                <button style={styles.actionBtn}>פרטים נוספים</button>
                <button style={{ ...styles.actionBtn, ...styles.loanBtn }}>השאל עכשיו</button>
              </div>
            </div>
          ))}
        </div>
      </main>
    </div>
  );
};

const styles = {
  container: {
    fontFamily: 'Segoe UI, Tahoma, Geneva, Verdana, sans-serif',
    direction: 'rtl',
    backgroundColor: '#F3F4F6',
    minHeight: '100vh',
    padding: '24px',
    boxSizing: 'border-box'
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    background: 'linear-gradient(135deg, #6366F1 0%, #A855F7 100%)',
    padding: '28px 36px',
    borderRadius: '20px',
    color: '#FFFFFF',
    boxShadow: '0 10px 25px -5px rgba(99, 102, 241, 0.4)',
    marginBottom: '28px'
  },
  title: {
    margin: 0,
    fontSize: '28px',
    fontWeight: '800'
  },
  subtitle: {
    margin: '6px 0 0 0',
    opacity: 0.9,
    fontSize: '15px'
  },
  primaryBtn: {
    backgroundColor: '#FFFFFF',
    color: '#4F46E5',
    border: 'none',
    padding: '12px 24px',
    borderRadius: '12px',
    fontWeight: '700',
    fontSize: '15px',
    cursor: 'pointer',
    boxShadow: '0 4px 12px rgba(0,0,0,0.1)'
  },
  statsGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))',
    gap: '20px',
    marginBottom: '32px'
  },
  statCard: {
    borderRadius: '16px',
    padding: '20px',
    color: '#FFFFFF',
    display: 'flex',
    alignItems: 'center',
    gap: '16px',
    boxShadow: '0 8px 16px rgba(0,0,0,0.06)'
  },
  statIcon: {
    fontSize: '32px',
    backgroundColor: 'rgba(255, 255, 255, 0.25)',
    padding: '12px',
    borderRadius: '14px'
  },
  statValue: {
    fontSize: '26px',
    fontWeight: '800'
  },
  statTitle: {
    fontSize: '14px',
    opacity: 0.95
  },
  mainContent: {
    backgroundColor: '#FFFFFF',
    borderRadius: '20px',
    padding: '28px',
    boxShadow: '0 4px 20px rgba(0,0,0,0.03)'
  },
  filterBar: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: '24px',
    flexWrap: 'wrap',
    gap: '16px'
  },
  tabs: {
    display: 'flex',
    gap: '8px',
    backgroundColor: '#F3F4F6',
    padding: '6px',
    borderRadius: '12px'
  },
  tabButton: {
    padding: '8px 18px',
    border: 'none',
    borderRadius: '8px',
    backgroundColor: 'transparent',
    color: '#4B5563',
    fontWeight: '600',
    cursor: 'pointer'
  },
  activeTab: {
    backgroundColor: '#FFFFFF',
    color: '#4F46E5',
    boxShadow: '0 2px 8px rgba(0,0,0,0.08)'
  },
  searchInput: {
    padding: '10px 16px',
    borderRadius: '10px',
    border: '1px solid #E5E7EB',
    width: '280px',
    fontSize: '14px',
    outline: 'none'
  },
  itemsGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(260px, 1fr))',
    gap: '20px'
  },
  itemCard: {
    border: '1px solid #F3F4F6',
    borderRadius: '16px',
    padding: '20px',
    backgroundColor: '#FAFAFA',
    display: 'flex',
    flexDirection: 'column',
    justifyContent: 'space-between'
  },
  cardHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: '12px'
  },
  statusBadge: {
    color: '#FFFFFF',
    padding: '4px 10px',
    borderRadius: '20px',
    fontSize: '12px',
    fontWeight: '700'
  },
  copyId: {
    fontSize: '12px',
    color: '#9CA3AF'
  },
  itemName: {
    margin: '0 0 6px 0',
    fontSize: '17px',
    color: '#1F2937'
  },
  itemCategory: {
    margin: '0 0 16px 0',
    fontSize: '14px',
    color: '#6B7280'
  },
  cardFooter: {
    display: 'flex',
    gap: '8px'
  },
  actionBtn: {
    flex: 1,
    padding: '8px 0',
    border: '1px solid #E5E7EB',
    borderRadius: '8px',
    backgroundColor: '#FFFFFF',
    color: '#374151',
    fontWeight: '600',
    cursor: 'pointer',
    fontSize: '13px'
  },
  loanBtn: {
    backgroundColor: '#4F46E5',
    color: '#FFFFFF',
    border: 'none'
  }
};

export default Dashboard;