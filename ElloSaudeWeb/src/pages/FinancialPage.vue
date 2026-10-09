<template>
  <div class="financial-page">
    <div class="page-header mb-4">
      <div>
        <h1 class="text-3xl font-bold text-900 m-0">Controle Financeiro</h1>
        <p class="text-500 m-0 mt-1">Lançamentos manuais para gestão financeira da clínica; nenhum pagamento é processado pelo sistema.</p>
      </div>
      <Button label="Atualizar dados" icon="pi pi-refresh" outlined :loading="loading" @click="loadFinancial" />
    </div>

    <p v-if="errorMessage" class="error-message" role="alert">{{ errorMessage }}</p>

    <div class="filters">
      <label>
        De
        <input v-model="startDate" type="date" @change="loadFinancial" />
      </label>
      <label>
        Até
        <input v-model="endDate" type="date" @change="loadFinancial" />
      </label>
      <label v-if="!isSecretary">
        Profissional
        <Dropdown
          v-model="professionalId"
          :options="professionals"
          optionLabel="name"
          optionValue="id"
          placeholder="Todos"
          showClear
          @change="loadFinancial"
        />
      </label>
    </div>

    <div class="grid mb-4">
      <div class="col-12 md:col-4">
        <Card class="metric-card bg-emerald-500 text-white p-shadow-3">
          <template #content>
            <div class="flex justify-content-between align-items-center">
              <div>
                <span class="text-sm font-semibold opacity-90 block">TOTAL RECEBIDO</span>
                <span class="text-3xl font-bold block mt-2">{{ formatCurrency(summary.totalReceived) }}</span>
              </div>
              <i class="pi pi-check-circle text-5xl opacity-80"></i>
            </div>
            <span class="text-xs opacity-90 mt-2 block">{{ summary.paidCount || 0 }} lançamentos quitados</span>
          </template>
        </Card>
      </div>

      <div class="col-12 md:col-4">
        <Card class="metric-card bg-amber-500 text-white p-shadow-3">
          <template #content>
            <div class="flex justify-content-between align-items-center">
              <div>
                <span class="text-sm font-semibold opacity-90 block">SALDO PENDENTE</span>
                <span class="text-3xl font-bold block mt-2">{{ formatCurrency(summary.totalPending) }}</span>
              </div>
              <i class="pi pi-clock text-5xl opacity-80"></i>
            </div>
            <span class="text-xs opacity-90 mt-2 block">{{ summary.pendingCount || 0 }} lançamentos em aberto</span>
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
            <span v-if="!isSecretary" class="text-xs opacity-90 mt-2 block">
              {{ summary.freeReturnCount || 0 }} retornos gratuitos
            </span>
            <span v-else class="text-xs opacity-90 mt-2 block">Resumo financeiro do período</span>
          </template>
        </Card>
      </div>
    </div>

    <Card class="p-shadow-3 transactions-card">
      <template #title>Lançamentos financeiros</template>
      <template #content>
        <p v-if="isSecretary" class="text-500">A Secretaria acessa somente os lançamentos pendentes ou parcialmente pagos necessários para registrar recebimentos.</p>
        <div v-if="loading" class="empty-state" role="status">Carregando lançamentos...</div>
        <div v-else-if="!transactions.length" class="empty-state">Nenhum lançamento encontrado no período.</div>
        <div v-else class="table-scroll">
          <table>
            <thead>
              <tr>
                <th>Paciente</th>
                <th>Profissional</th>
                <th>Previsto</th>
                <th>Recebido</th>
                <th>Saldo</th>
                <th>Status</th>
                <th>Forma</th>
                <th>Data</th>
                <th>Ações</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="transaction in transactions" :key="transaction.id">
                <td>{{ transaction.patientName }}</td>
                <td>{{ transaction.professionalName }}</td>
                <td>{{ formatCurrency(transaction.expectedAmount) }}</td>
                <td>{{ formatCurrency(transaction.amountPaid) }}</td>
                <td>{{ formatCurrency(transaction.expectedAmount - transaction.amountPaid) }}</td>
                <td>{{ statusLabel(transaction.status) }}</td>
                <td>{{ methodLabel(transaction.method) }}</td>
                <td>{{ formatDate(transaction.paymentDate || transaction.createdAt) }}</td>
                <td class="actions">
                  <Button
                    v-if="canRegisterPayment(transaction)"
                    label="Registrar recebimento"
                    icon="pi pi-wallet"
                    size="small"
                    @click="openPaymentDialog(transaction)"
                  />
                  <Button
                    v-if="canCancelTransaction(transaction)"
                    label="Cancelar lançamento"
                    severity="danger"
                    text
                    size="small"
                    @click="cancelTransaction(transaction)"
                  />
                </td>
              </tr>
            </tbody>
          </table>
        </div>
        <div v-if="!isSecretary && paymentMethods.length" class="breakdowns">
          <section>
            <h3>Recebimentos por forma</h3>
            <ul>
              <li v-for="item in paymentMethods" :key="item.method">
                {{ methodLabel(item.method) }} — {{ formatCurrency(item.totalAmount) }} ({{ item.count }})
              </li>
            </ul>
          </section>
          <section>
            <h3>Recebimentos por profissional</h3>
            <ul>
              <li v-for="item in paymentProfessionals" :key="item.professionalId">
                {{ item.professionalName }} — {{ formatCurrency(item.totalReceived) }} ({{ item.paidCount }})
              </li>
            </ul>
          </section>
        </div>
      </template>
    </Card>

    <Dialog v-model:visible="paymentDialogVisible" header="Registrar recebimento manual" modal :style="{ width: 'min(32rem, 95vw)' }">
      <p v-if="selectedTransaction">
        Saldo de {{ formatCurrency(remainingAmount) }} para {{ selectedTransaction.patientName }}.
      </p>
      <div class="payment-form">
        <label for="payment-amount">Valor recebido</label>
        <InputNumber
          id="payment-amount"
          v-model="paymentAmount"
          mode="currency"
          currency="BRL"
          locale="pt-BR"
          :min="0.01"
          :max="remainingAmount"
        />
        <label for="payment-method">Forma de pagamento</label>
        <Dropdown
          id="payment-method"
          v-model="paymentMethodId"
          :options="paymentMethodOptions"
          optionLabel="label"
          optionValue="value"
          :disabled="Boolean(selectedTransaction?.amountPaid)"
          placeholder="Selecione uma forma"
        />
        <label for="payment-notes">Observações (opcional)</label>
        <textarea id="payment-notes" v-model="paymentNotes" maxlength="500" rows="3"></textarea>
      </div>
      <template #footer>
        <Button label="Fechar" text @click="paymentDialogVisible = false" />
        <Button label="Salvar recebimento" icon="pi pi-check" :loading="savingPayment" @click="registerPayment" />
      </template>
    </Dialog>
  </div>
</template>

<script setup>
import { computed, ref, onMounted } from 'vue';
import Button from 'primevue/button';
import Card from 'primevue/card';
import Dialog from 'primevue/dialog';
import Dropdown from 'primevue/dropdown';
import InputNumber from 'primevue/inputnumber';
import { financialService } from '@/api/financial.service';
import { professionalsService } from '@/api/professionals.service';
import { useAuthStore } from '@/stores/auth.store';

const authStore = useAuthStore();
const isSecretary = computed(() => authStore.user?.role === 'Secretaria');
const today = new Date();
const startDate = ref(`${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, '0')}-01`);
const endDate = ref(formatDateInput(today));
const professionalId = ref('');
const professionals = ref([]);
const summary = ref({});
const transactions = ref([]);
const paymentMethods = ref([]);
const paymentProfessionals = ref([]);
const loading = ref(false);
const savingPayment = ref(false);
const errorMessage = ref('');
const paymentDialogVisible = ref(false);
const selectedTransaction = ref(null);
const paymentAmount = ref(null);
const paymentMethodId = ref(null);
const paymentNotes = ref('');

const paymentMethodOptions = [
  { value: 1, label: 'Pix' },
  { value: 2, label: 'Dinheiro' },
  { value: 3, label: 'Cartão de débito' },
  { value: 4, label: 'Cartão de crédito' },
  { value: 5, label: 'Transferência' },
  { value: 6, label: 'Convênio' },
  { value: 7, label: 'Outro' }
];

const methodIds = {
  Pix: 1,
  Dinheiro: 2,
  CartaoDebito: 3,
  CartaoCredito: 4,
  Transferencia: 5,
  Convenio: 6,
  Outro: 7
};

const remainingAmount = computed(() => selectedTransaction.value
  ? Math.max(0, selectedTransaction.value.expectedAmount - selectedTransaction.value.amountPaid)
  : 0);

onMounted(async () => {
  if (!isSecretary.value) {
    try {
      professionals.value = await professionalsService.getAll();
    } catch (err) {
      errorMessage.value = err.response?.data?.detail || 'Não foi possível carregar os profissionais.';
    }
  }
  await loadFinancial();
});

async function loadFinancial() {
  if (startDate.value && endDate.value && startDate.value > endDate.value) {
    errorMessage.value = 'A data inicial deve ser anterior ou igual à data final.';
    return;
  }

  loading.value = true;
  errorMessage.value = '';
  try {
    if (isSecretary.value) {
      const [result, pending] = await Promise.all([
        financialService.getSummary(startDate.value, endDate.value),
        financialService.getPendingPayments(startDate.value, endDate.value)
      ]);
      summary.value = {
        ...result,
        paidCount: result.completedCount,
        freeReturnCount: 0
      };
      transactions.value = pending;
      paymentMethods.value = [];
      paymentProfessionals.value = [];
    } else {
      const result = await financialService.getReport(
        startDate.value,
        endDate.value,
        professionalId.value
      );
      summary.value = result;
      transactions.value = result.transactions;
      paymentMethods.value = result.byPaymentMethod;
      paymentProfessionals.value = result.byProfessional;
    }
  } catch (err) {
    errorMessage.value = err.response?.data?.detail || 'Não foi possível carregar os dados financeiros.';
  } finally {
    loading.value = false;
  }
}

function formatCurrency(val) {
  return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(val || 0);
}

function formatDateInput(date) {
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
}

function formatDate(value) {
  if (!value) return '—';
  return new Intl.DateTimeFormat('pt-BR', { timeZone: 'UTC', dateStyle: 'short' }).format(new Date(value));
}

function methodLabel(method) {
  return paymentMethodOptions.find(option => methodIds[method] === option.value)?.label || method || '—';
}

function statusLabel(status) {
  return ({
    Pendente: 'Pendente',
    ParcialmentePago: 'Parcialmente pago',
    Pago: 'Pago',
    GratuitoRetorno: 'Retorno gratuito',
    Cancelado: 'Cancelado'
  })[status] || status;
}

function canRegisterPayment(transaction) {
  return ['Admin', 'Secretaria'].includes(authStore.user?.role)
    && ['Pendente', 'ParcialmentePago'].includes(transaction.status)
    && transaction.expectedAmount > transaction.amountPaid;
}

function canCancelTransaction(transaction) {
  return ['Admin', 'Secretaria'].includes(authStore.user?.role)
    && transaction.status === 'Pendente'
    && transaction.amountPaid === 0;
}

function openPaymentDialog(transaction) {
  selectedTransaction.value = transaction;
  paymentAmount.value = remainingAmount.value;
  paymentMethodId.value = transaction.method ? methodIds[transaction.method] : 1;
  paymentNotes.value = '';
  paymentDialogVisible.value = true;
}

async function registerPayment() {
  if (!selectedTransaction.value || !paymentAmount.value || paymentAmount.value <= 0
    || paymentAmount.value > remainingAmount.value || !paymentMethodId.value) {
    errorMessage.value = 'Informe um valor válido, dentro do saldo pendente, e uma forma de pagamento.';
    return;
  }

  savingPayment.value = true;
  errorMessage.value = '';
  try {
    await financialService.registerPayment(selectedTransaction.value.id, {
      amountPaid: paymentAmount.value,
      paymentMethodId: paymentMethodId.value,
      notes: paymentNotes.value.trim() || null
    });
    paymentDialogVisible.value = false;
    await loadFinancial();
  } catch (err) {
    errorMessage.value = err.response?.data?.detail || 'Não foi possível registrar o recebimento.';
  } finally {
    savingPayment.value = false;
  }
}

async function cancelTransaction(transaction) {
  const reason = window.prompt('Informe o motivo do cancelamento do lançamento:');
  if (!reason?.trim()) return;

  errorMessage.value = '';
  try {
    await financialService.cancelPayment(transaction.id, reason.trim());
    await loadFinancial();
  } catch (err) {
    errorMessage.value = err.response?.data?.detail || 'Não foi possível cancelar o lançamento.';
  }
}
</script>

<style scoped>
.filters {
  display: flex;
  flex-wrap: wrap;
  align-items: end;
  gap: 1rem;
  margin-bottom: 1.5rem;
}
.filters label,
.payment-form {
  display: grid;
  gap: 0.4rem;
}
.filters input,
.payment-form textarea {
  min-height: 2.75rem;
  padding: 0.5rem;
  border: 1px solid #cbd5e1;
  border-radius: 6px;
  font: inherit;
}
.filters :deep(.p-dropdown) {
  min-width: 14rem;
}
.payment-form {
  gap: 0.65rem;
}
.payment-form :deep(.p-dropdown) {
  width: 100%;
}
.table-scroll {
  overflow-x: auto;
}
table {
  width: 100%;
  border-collapse: collapse;
  min-width: 960px;
}
th,
td {
  padding: 0.75rem;
  border-bottom: 1px solid #e5e7eb;
  text-align: left;
}
th {
  color: #475569;
  font-size: 0.875rem;
}
.actions {
  min-width: 13rem;
}
.breakdowns {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(16rem, 1fr));
  gap: 1rem;
  margin-top: 1.5rem;
}
.breakdowns h3 {
  font-size: 1rem;
}
.breakdowns ul {
  padding-left: 1.25rem;
}
.empty-state {
  padding: 2rem;
  color: #64748b;
  text-align: center;
}
.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}
.metric-card {
  border-radius: 12px;
}
</style>
