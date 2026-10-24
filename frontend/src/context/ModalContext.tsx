import React, { createContext, useContext, useState, ReactNode } from 'react';
import { TransactionOutput } from '../api/types';
import { PopupDeCarga } from '../components/modals/PopupDeCarga';

interface ModalContextType {
  openCreateModal: () => void;
  openEditModal: (tx: TransactionOutput) => void;
  closeModal: () => void;
}

const ModalContext = createContext<ModalContextType | undefined>(undefined);

export const ModalProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [isOpen, setIsOpen] = useState(false);
  const [editingTransaction, setEditingTransaction] = useState<TransactionOutput | null>(null);

  const openCreateModal = () => {
    setEditingTransaction(null);
    setIsOpen(true);
  };

  const openEditModal = (tx: TransactionOutput) => {
    setEditingTransaction(tx);
    setIsOpen(true);
  };

  const closeModal = () => {
    setIsOpen(false);
    setEditingTransaction(null);
  };

  return (
    <ModalContext.Provider value={{ openCreateModal, openEditModal, closeModal }}>
      {children}
      <PopupDeCarga
        isOpen={isOpen}
        onClose={closeModal}
        editingTransaction={editingTransaction}
      />
    </ModalContext.Provider>
  );
};

export function useModalCarga() {
  const context = useContext(ModalContext);
  if (!context) {
    throw new Error('useModalCarga debe ser utilizado dentro de un ModalProvider');
  }
  return context;
}
