<template>
  <div class="financial-page">
    <div class="page-header mb-4">
      <div>
        <h1 class="text-3xl font-bold text-900 m-0">Controle Financeiro</h1>
        <p class="text-500 m-0 mt-1">Resumo de faturamento, recebimentos e consultas pendentes (RF12 / RF13)</p>
      </div>
      <Button label="Atualizar Dados" icon="pi pi-refresh" class="p-button-outlined p-button-primary" @click="loadSummary" />
    </div>

    <!-- Cards de Resumo Financeiro -->
    <div class="grid mb-4">
      <div class="col-12 md:col-4">
        <Card class="metric-card bg-emerald-500 text-white p-shadow-3">
          <template #content>
            <div class="flex justify-content-between align-items-center">
              <div>
                <span class="text-sm font-semibold opacity-90 block">TOTAL RECEBIDO (PAGO)</span>
                <span class="text-3xl font-bold block mt-2">{{ formatCurrency(summary.totalReceived) }}</span>
              </div>
              <i class="pi pi-check-circle text-5xl opacity-80"></i>
            </div>
            <span class="text-xs opacity-90 mt-2 block">{{ summary.completedAppointments || 0 }} consultas concluídas</span>
          </template>
        </Card>
      </div>

      <div class="col-12 md:col-4">
        <Card class="metric-card bg-amber-500 text-white p-shadow-3">
          <template #content>
            <div class="flex justify-content-between align-items-center">
              <div>
                <span class="text-sm font-semibold opacity-90 block">TOTAL PENDENTE</span>
                <span class="text-3xl font-bold block mt-2">{{ formatCurrency(summary.totalPending) }}</span>
              </div>
              <i class="pi pi-clock text-5xl opacity-80"></i>
            </div>
            <span class="text-xs opacity-90 mt-2 block">{{ summary.pendingAppointments || 0 }} consultas aguardando</span>
          </template>
        </Card>
      </div>

      <div class="col-12 md:col-4">
        <Card class="metric-card bg-indigo-600 text-white p-shadow-3">
          <template #content>
            <div class="flex justify-content-between align-items-center">
              <div>
                <span class="text-sm font-semibold opacity-90 block">FATURAMENTO TOTAL</span>
                <span class="text-3xl font-bold block mt-2">{{ formatCurrency(summary.totalRevenue) }}</span>
              </div>
              <i class="pi pi-chart-line text-5xl opacity-80"></i>
            </div>
            <span class="text-xs opacity-90 mt-2 block">Projeção geral de receita</span>
          </template>
        </Card>
      </div>
    </div>

    <!-- Tabela de Relatório Geral -->
    <Card class="p-shadow-3">
      <template #title>
        <div class="flex justify-content-between align-items-center">
          <span class="text-xl font-bold text-900"><i class="pi pi-list mr-2 text-primary"></i> Relatório Consolidado de Atendimentos</span>
        </div>
      </template>
      <template #content>
        <div class="surface-100 p-4 border-round flex flex-column md:flex-row justify-content-around text-center">
          <div class="my-2 md:my-0">
            <span class="text-500 text-sm font-medium block">Total de Atendimentos</span>
            <span class="text-2xl font-bold text-900">{{ (summary.completedAppointments || 0) + (summary.pendingAppointments || 0) }}</span>
          </div>
          <div class="my-2 md:my-0">
            <span class="text-500 text-sm font-medium block">Taxa de Conclusão</span>
            <span class="text-2xl font-bold text-emerald-600">{{ getCompletionRate() }}%</span>
          </div>
          <div class="my-2 md:my-0">
            <span class="text-500 text-sm font-medium block">Ticket Médio por Consulta</span>
            <span class="text-2xl font-bold text-indigo-600">R$ 150,00</span>
          </div>
        </div>
      </template>
    </Card>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue';
import Button from 'primevue/button';
import Card from 'primevue/card';
import { financialService } from '@/api/financial.service';

const summary = ref({
  totalReceived: 0,
  totalPending: 0,
  totalRevenue: 0,
  completedAppointments: 0,
  pendingAppointments: 0
});

onMounted(async () => {
  await loadSummary();
});

async function loadSummary() {
  try {
    summary.value = await financialService.getSummary();
  } catch (err) {
    console.error('Erro ao carregar resumo financeiro', err);
  }
}

function formatCurrency(val) {
  if (!val) return 'R$ 0,00';
  return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(val);
}

function getCompletionRate() {
  const total = (summary.value.completedAppointments || 0) + (summary.value.pendingAppointments || 0);
  if (total === 0) return 0;
  return Math.round((summary.value.completedAppointments / total) * 100);
}
</script>

<style scoped>
.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}
.metric-card {
  border-radius: 12px;
}
</style>
