import { useMutation, useQueryClient } from '@tanstack/react-query';
import { apiService } from '../api/apiService';
import {
  CategoryCreateInput,
  TarjetaCreateInput,
  TransactionCreateInput,
  TransactionUpdateInput,
  AplicarDesde,
} from '../api/types';

export function useMovimientoMutations() {
  const queryClient = useQueryClient();

  const invalidateFinancialData = () => {
    queryClient.invalidateQueries({ queryKey: ['dashboard'] });
    queryClient.invalidateQueries({ queryKey: ['reportes'] });
    queryClient.invalidateQueries({ queryKey: ['movimientos'] });
    queryClient.invalidateQueries({ queryKey: ['pendientes'] });
  };

  const createMovimiento = useMutation({
    mutationFn: (input: TransactionCreateInput) => apiService.movimientos.create(input),
    onSuccess: () => {
      invalidateFinancialData();
    },
  });

  const updateMovimiento = useMutation({
    mutationFn: ({
      id,
      input,
      aplicarDesde,
    }: {
      id: number;
      input: TransactionUpdateInput;
      aplicarDesde?: AplicarDesde;
    }) => apiService.movimientos.update(id, input, aplicarDesde),
    onSuccess: (_, variables) => {
      invalidateFinancialData();
      queryClient.invalidateQueries({ queryKey: ['movimiento', variables.id] });
    },
  });

  const deleteMovimiento = useMutation({
    mutationFn: (id: number) => apiService.movimientos.delete(id),
    onSuccess: () => {
      invalidateFinancialData();
    },
  });

  const adelantarCuotas = useMutation({
    mutationFn: ({ id, cantidad }: { id: number; cantidad: number }) =>
      apiService.movimientos.adelantarCuotas(id, cantidad),
    onSuccess: (_, variables) => {
      invalidateFinancialData();
      queryClient.invalidateQueries({ queryKey: ['pagosCuota', variables.id] });
    },
  });

  const createCategoria = useMutation({
    mutationFn: (input: CategoryCreateInput) => apiService.categorias.create(input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['categorias'] });
    },
  });

  const createTarjeta = useMutation({
    mutationFn: (input: TarjetaCreateInput) => apiService.tarjetas.create(input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tarjetas'] });
    },
  });

  return {
    createMovimiento,
    updateMovimiento,
    deleteMovimiento,
    adelantarCuotas,
    createCategoria,
    createTarjeta,
  };
}
