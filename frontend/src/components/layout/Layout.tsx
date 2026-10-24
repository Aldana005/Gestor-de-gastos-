import React from 'react';
import { Outlet } from 'react-router-dom';
import { Header } from './Header';
import { DesktopSidebar, BottomNav } from './Navbar';
import { FloatingActionButton } from '../common/FloatingActionButton';
import { useModalCarga } from '../../context/ModalContext';

export const Layout: React.FC = () => {
  const { openCreateModal } = useModalCarga();

  return (
    <div className="min-h-screen min-h-[100dvh] flex flex-col bg-brand-900 text-text-primary selection:bg-brand-500 selection:text-brand-900">
      <Header />

      <div className="flex-1 flex w-full max-w-7xl mx-auto">
        <DesktopSidebar />

        <main className="flex-1 px-4 py-5 sm:px-6 sm:py-7 md:px-8 max-w-full pb-28 md:pb-12 overflow-x-hidden">
          <Outlet />
        </main>
      </div>

      <BottomNav />
      <FloatingActionButton onClick={openCreateModal} />
    </div>
  );
};
