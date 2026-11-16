import type { ReactNode } from 'react';
import { Card, Typography } from 'antd';
import { Link } from 'react-router-dom';

export default function AuthCard({ title, children }: { title: string; children: ReactNode }) {
  return (
    <div
      style={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        padding: 16,
        background: 'linear-gradient(135deg, #e6f4ff 0%, #f5f9ff 50%, #ffffff 100%)',
      }}
    >
      <Card style={{ width: '100%', maxWidth: 400 }}>
        <Link to="/" style={{ fontSize: 28, fontWeight: 700, display: 'block', textAlign: 'center' }}>
          E-Tax
        </Link>
        <Typography.Title level={4} style={{ textAlign: 'center', marginTop: 8, marginBottom: 24 }}>
          {title}
        </Typography.Title>
        {children}
      </Card>
    </div>
  );
}
