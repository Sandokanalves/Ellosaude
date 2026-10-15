<template>
  <div class="financial-page">
    <Toast />
    
    <div class="page-header mb-4 flex justify-content-between align-items-center">
      <div>
        <h1 class="text-3xl font-bold text-900 m-0">Módulo Financeiro & DRE</h1>
        <p class="text-500 m-0 mt-1">Gestão de caixa diário, baixas de pagamentos, controle de despesas operacionais e cálculo de DRE/lucro líquido real.</p>
      </div>
      <Button label="Atualizar Dados" icon="pi pi-refresh" outlined :loading="loading" @click="refreshAll" />
    </div>

    <TabView v-model:activeIndex="activeTab">
      <!-- ABA 1: LANÇAMENTOS E RECEBIMENTOS -->
      <TabPanel header="1. Faturamento & Recebimentos">
        <div class="filters flex flex-wrap gap-3 align-items-center mb-3">
          <label class="flex flex-column text-sm font-semibold text-700">
            Data Inicial
            <input v-model="startDate" type="date" class="p-inputtext p-component p-2 mt-1" @change="loadFinancial" />
          </label>
          <label class="flex flex-column text-sm font-semibold text-700">
            Data Final
            <input v-model="endDate" type="date" class="p-inputtext p-component p-2 mt-1" @change="loadFinancial" />
          </label>
          <label v-if="!isSecretary" class="flex flex-column text-sm font-semibold text-700">
            Profissional
            <Dropdown
              v-model="professionalId"
              :options="professionals"
              optionLabel="name"
              optionValue="id"
              placeholder="Todos os profissionais"
              showClear
              class="mt-1"
              @change="loadFinancial"
            />
          </label>
        </div>

        <div class="grid mb-4">
          <div class="col-12 md:col-4">
            <Card class="metric-card bg-emerald-600 text-white p-shadow-2">
              <template #content>
                <div class="flex justify-content-between align-items-center">
                  <div>
                    <span class="text-xs font-semibold uppercase opacity-90 block">TOTAL RECEBIDO EFETIVO</span>
                    <span class="text-3xl font-bold block mt-1">{{ formatCurrency(summary.totalReceived) }}</span>
                  </div>
                  <i class="pi pi-check-circle text-4xl opacity-80"></i>
                </div>
                <span class="text-xs opacity-90 mt-2 block">{{ summary.paidCount || 0 }} recebimentos baixados</span>
              </template>
            </Card>
          </div>

          <div class="col-12 md:col-4">
            <Card class="metric-card bg-amber-600 text-white p-shadow-2">
              <template #content>
                <div class="flex justify-content-between align-items-center">
                  <div>
                    <span class="text-xs font-semibold uppercase opacity-90 block">SALDO PENDENTE</span>
                    <span class="text-3xl font-bold block mt-1">{{ formatCurrency(summary.totalPending) }}</span>
                  </div>
                  <i class="pi pi-clock text-4xl opacity-80"></i>
                </div>
                <span class="text-xs opacity-90 mt-2 block">{{ summary.pendingCount || 0 }} consultas aguardando baixa</span>
              </template>
            </Card>
          </div>

          <div class="col-12 md:col-4">
            <Card class="metric-card bg-indigo-600 text-white p-shadow-2">
              <template #content>
                <div class="flex justify-content-between align-items-center">
                  <div>
                    <span class="text-xs font-semibold uppercase opacity-90 block">PREVISÃO TOTAL</span>
                    <span class="text-3xl font-bold block mt-1">{{ formatCurrency(summary.totalRevenue) }}</span>
                  </div>
                  <i class="pi pi-chart-line text-4xl opacity-80"></i>
                </div>
                <span class="text-xs opacity-90 mt-2 block">{{ summary.freeReturnCount || 0 }} consultas de retorno gratuito</span>
              </template>
            </Card>
          </div>
        </div>

        <Card class="p-shadow-2">
          <template #title>
            <span class="text-lg font-bold text-800">Lançamentos Financeiros dos Atendimentos</span>
          </template>
          <template #content>
            <div v-if="loading" class="text-center p-4 text-500">Carregando lançamentos...</div>
            <div v-else-if="!transactions.length" class="text-center p-4 surface-50 border-round text-500">Nenhum lançamento encontrado no período.</div>
            <div v-else class="overflow-x-auto">
              <table class="w-full text-sm">
                <thead>
                  <tr class="border-bottom-2 surface-border text-left">
                    <th class="p-2">Paciente</th>
                    <th class="p-2">Profissional</th>
                    <th class="p-2">Previsto</th>
                    <th class="p-2">Recebido</th>
                    <th class="p-2">Saldo</th>
                    <th class="p-2">Status</th>
                    <th class="p-2">Forma</th>
                    <th class="p-2 text-right">Ações</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="t in transactions" :key="t.id" class="border-bottom-1 surface-border hover:surface-50">
                    <td class="p-2 font-semibold text-900">{{ t.patientName || 'Paciente' }}</td>
                    <td class="p-2 text-700">{{ t.professionalName || '-' }}</td>
                    <td class="p-2">{{ formatCurrency(t.expectedAmount) }}</td>
                    <td class="p-2 text-green-600 font-bold">{{ formatCurrency(t.amountPaid) }}</td>
                    <td class="p-2 text-amber-600">{{ formatCurrency(t.remainingBalance) }}</td>
                    <td class="p-2">
                      <Tag :value="formatStatus(t.status)" :severity="getStatusSeverity(t.status)" />
                    </td>
                    <td class="p-2 text-600">{{ formatPaymentMethod(t.paymentMethodId) }}</td>
                    <td class="p-2 text-right">
                      <div class="flex justify-content-end gap-1">
                        <Button
                          v-if="isSecretary && (t.status === 1 || t.remainingBalance > 0)"
                          label="Baixar"
                          icon="pi pi-check"
                          size="small"
                          severity="success"
                          @click="openPaymentModal(t)"
                        />
                        <Button
                          v-if="!isSecretary && t.status === 1"
                          label="Retorno"
                          icon="pi pi-replay"
                          size="small"
                          outlined
                          severity="secondary"
                          @click="openFreeReturnModal(t)"
                        />
                      </div>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </template>
        </Card>
      </TabPanel>

      <!-- ABA 2: CAIXA DIÁRIO (ABERTURA & FECHAMENTO) -->
      <TabPanel header="2. Caixa Diário">
        <div class="grid">
          <div class="col-12 md:col-6">
            <Card class="surface-card border-1 surface-border border-round h-full p-shadow-2">
              <template #title>
                <div class="flex justify-content-between align-items-center">
                  <span class="text-base font-bold text-800">Status do Caixa Atual</span>
                  <Tag :value="cashSession?.status || 'Fechado'" :severity="cashSession?.status === 'Aberto' ? 'success' : 'danger'" />
                </div>
              </template>
              <template #content>
                <div v-if="cashSession && cashSession.status === 'Aberto'">
                  <div class="p-3 bg-green-50 border-round border-1 border-green-200 mb-3">
                    <span class="text-xs text-green-800 font-semibold block">SALDO TOTAL EM CAIXA ATUAL</span>
                    <strong class="text-4xl text-green-700 block mt-1">{{ formatCurrency(cashSession.currentBalance) }}</strong>
                  </div>

                  <div class="text-sm">
                    <p class="my-2"><strong>Aberto em:</strong> {{ formatDateTime(cashSession.openedAt) }}</p>
                    <p class="my-2"><strong>Fundo de Caixa Inicial:</strong> {{ formatCurrency(cashSession.openingBalance) }}</p>
                    <p class="my-2"><strong>Recebido Hoje:</strong> {{ formatCurrency(cashSession.totalReceivedToday) }}</p>
                    <p v-if="cashSession.notes" class="my-2 text-600"><strong>Observações:</strong> {{ cashSession.notes }}</p>
                  </div>

                  <div class="mt-4">
                    <Button label="Fechar Caixa do Dia" icon="pi pi-lock" severity="danger" class="w-full" @click="showCloseCashModal = true" />
                  </div>
                </div>

                <div v-else class="text-center py-5">
                  <i class="pi pi-lock text-5xl text-500 mb-2 block"></i>
                  <h3 class="text-900 m-0 font-bold">O caixa está fechado</h3>
                  <p class="text-500 text-sm mt-1 mb-4">Abra o caixa no início do expediente para registrar recebimentos e conferir troco.</p>
                  <Button label="Abrir Caixa Diário" icon="pi pi-lock-open" severity="success" @click="showOpenCashModal = true" />
                </div>
              </template>
            </Card>
          </div>

          <div class="col-12 md:col-6">
            <Card class="surface-card border-1 surface-border border-round h-full p-shadow-2">
              <template #title>
                <span class="text-base font-bold text-800">Regras de Gestão de Caixa</span>
              </template>
              <template #content>
                <ul class="text-sm text-700 line-height-3 pl-3 m-0">
                  <li><strong>Abertura Obrigatória:</strong> Registre o fundo de troco em espécie disponível na gaveta da recepção.</li>
                  <li><strong>Baixas Efetivas:</strong> Cada pagamento marcado como "Pago" no sistema atualiza o saldo calculado do dia.</li>
                  <li><strong>Conferência Cega no Fechamento:</strong> Ao final do expediente, conte o total físico em dinheiro e informe o valor conferido para registrar eventuais quebras ou sangrias.</li>
                  <li><strong>Auditoria Completa:</strong> Horário de abertura, fechamento e operador responsável são gravados com assinatura de auditoria.</li>
                </ul>
              </template>
            </Card>
          </div>
        </div>
      </TabPanel>

      <!-- ABA 3: DESPESAS DA CLÍNICA -->
      <TabPanel header="3. Despesas da Clínica">
        <Card class="p-shadow-2">
          <template #title>
            <div class="flex justify-content-between align-items-center">
              <span class="text-lg font-bold text-800">Custos e Despesas Operacionais</span>
              <Button label="+ Nova Despesa" icon="pi pi-plus" size="small" class="p-button-primary" @click="showExpenseModal = true" />
            </div>
          </template>
          <template #content>
            <DataTable :value="expenses" responsiveLayout="scroll" class="p-datatable-sm" emptyMessage="Nenhuma despesa cadastrada no período.">
              <Column field="description" header="Descrição" sortable>
                <template #body="{ data }">
                  <strong class="text-900">{{ data.description }}</strong>
                  <small v-if="data.notes" class="text-500 block">{{ data.notes }}</small>
                </template>
              </Column>
              <Column field="category" header="Categoria" sortable>
                <template #body="{ data }">
                  <Tag :value="data.category" severity="secondary" />
                </template>
              </Column>
              <Column field="amount" header="Valor" sortable>
                <template #body="{ data }">
                  <strong class="text-red-600">{{ formatCurrency(data.amount) }}</strong>
                </template>
              </Column>
              <Column field="dueDate" header="Vencimento" sortable>
                <template #body="{ data }">{{ formatDate(data.dueDate) }}</template>
              </Column>
              <Column field="isPaid" header="Status">
                <template #body="{ data }">
                  <Tag :value="data.isPaid ? 'Paga' : 'Pendente'" :severity="data.isPaid ? 'success' : 'danger'" />
                </template>
              </Column>
              <Column header="Ações" style="width: 140px;">
                <template #body="{ data }">
                  <div class="flex gap-2">
                    <Button v-if="!data.isPaid" icon="pi pi-check" text rounded severity="success" size="small" @click="payExpense(data)" title="Marcar como Paga" />
                    <Button icon="pi pi-trash" text rounded severity="danger" size="small" @click="deleteExpense(data.id)" title="Excluir" />
                  </div>
                </template>
              </Column>
            </DataTable>
          </template>
        </Card>
      </TabPanel>

      <!-- ABA 4: DRE GERENCIAL E LUCRO LÍQUIDO -->
      <TabPanel header="4. DRE Simplificado & Lucro">
        <div class="grid">
          <div class="col-12 md:col-8">
            <Card class="surface-card border-1 surface-border border-round p-shadow-2 mb-3">
              <template #title>
                <div class="flex justify-content-between align-items-center">
                  <span class="text-lg font-bold text-800">Demonstrativo de Resultado do Exercício (DRE)</span>
                  <Tag :value="`Margem Líquida: ${dreData.profitMarginPercentage || 0}%`" severity="info" />
                </div>
              </template>
              <template #content>
                <div class="dre-table text-sm">
                  <div class="dre-row flex justify-content-between py-2 border-bottom-1 surface-border font-bold text-green-700">
                    <span>(+) RECEITA BRUTA OPERACIONAL (Consultas & Procedimentos)</span>
                    <span>{{ formatCurrency(dreData.grossRevenue) }}</span>
                  </div>
                  <div class="dre-row flex justify-content-between py-2 border-bottom-1 surface-border text-amber-700 pl-3">
                    <span>(-) Repasses e Comissões Médicas</span>
                    <span>{{ formatCurrency(dreData.totalCommissions) }}</span>
                  </div>
                  <div class="dre-row flex justify-content-between py-2 border-bottom-1 surface-border text-red-700 pl-3">
                    <span>(-) Despesas Operacionais (Aluguel, Luz, Insumos, etc.)</span>
                    <span>{{ formatCurrency(dreData.totalExpenses) }}</span>
                  </div>
                  <div class="dre-row flex justify-content-between py-3 border-top-2 surface-border font-extrabold text-xl" :class="dreData.netProfit >= 0 ? 'text-green-700 bg-green-50' : 'text-red-700 bg-red-50'">
                    <span>(=) LUCRO LÍQUIDO REAL</span>
                    <span>{{ formatCurrency(dreData.netProfit) }}</span>
                  </div>
                </div>
              </template>
            </Card>

            <!-- Detalhamento por Categoria -->
            <div class="grid">
              <div class="col-12 md:col-6">
                <Card class="surface-card border-1 surface-border border-round p-shadow-1">
                  <template #title><span class="text-sm font-bold text-700">Receitas por Forma de Pagamento</span></template>
                  <template #content>
                    <div v-for="item in dreData.revenueByMethod" :key="item.category" class="flex justify-content-between py-1 text-sm border-bottom-1 surface-border">
                      <span class="text-700">{{ item.category }}</span>
                      <strong class="text-green-600">{{ formatCurrency(item.amount) }}</strong>
                    </div>
                  </template>
                </Card>
              </div>

              <div class="col-12 md:col-6">
                <Card class="surface-card border-1 surface-border border-round p-shadow-1">
                  <template #title><span class="text-sm font-bold text-700">Despesas por Centro de Custo</span></template>
                  <template #content>
                    <div v-for="item in dreData.expensesByCategory" :key="item.category" class="flex justify-content-between py-1 text-sm border-bottom-1 surface-border">
                      <span class="text-700">{{ item.category }}</span>
                      <strong class="text-red-600">{{ formatCurrency(item.amount) }}</strong>
                    </div>
                  </template>
                </Card>
              </div>
            </div>
          </div>

          <div class="col-12 md:col-4">
            <Card class="bg-primary-900 text-white p-shadow-3 border-round h-full">
              <template #title>
                <div class="flex align-items-center gap-2">
                  <i class="pi pi-bolt text-yellow-400"></i>
                  <span class="text-base text-white">Indicadores de Desempenho</span>
                </div>
              </template>
              <template #content>
                <div class="py-2">
                  <span class="text-xs text-primary-200 block">MARGEM DE LUCRO LÍQUIDO</span>
                  <span class="text-4xl font-extrabold text-white block mt-1">{{ dreData.profitMarginPercentage || 0 }}%</span>
                </div>
                <div class="py-2 border-top-1 border-primary-700">
                  <span class="text-xs text-primary-200 block">TOTAL DE COMISSÕES ESTIMADAS</span>
                  <span class="text-2xl font-bold text-yellow-300 block mt-1">{{ formatCurrency(dreData.totalCommissions) }}</span>
                </div>
                <div class="py-2 border-top-1 border-primary-700">
                  <span class="text-xs text-primary-200 block">GASTOS OPERACIONAIS</span>
                  <span class="text-2xl font-bold text-red-300 block mt-1">{{ formatCurrency(dreData.totalExpenses) }}</span>
                </div>
              </template>
            </Card>
          </div>
        </div>
      </TabPanel>
    </TabView>

    <!-- MODAL BAIXAR PAGAMENTO -->
    <Dialog header="Registrar Pagamento" v-model:visible="showPaymentModal" :modal="true" :style="{ width: '450px' }">
      <div v-if="selectedTransaction" class="p-fluid">
        <p class="text-sm text-600 mb-3">Paciente: <strong>{{ selectedTransaction.patientName }}</strong></p>
        <div class="mb-3">
          <label class="font-semibold block mb-1">Valor a Receber (R$)</label>
          <InputNumber v-model="paymentForm.amountPaid" mode="currency" currency="BRL" locale="pt-BR" :min="0" :max="selectedTransaction.remainingBalance" />
        </div>
        <div class="mb-3">
          <label class="font-semibold block mb-1">Forma de Pagamento</label>
          <Dropdown v-model="paymentForm.paymentMethodId" :options="paymentMethods" optionLabel="label" optionValue="value" />
        </div>
        <div class="mb-3">
          <label class="font-semibold block mb-1">Observações / Comprovante</label>
          <InputText v-model="paymentForm.notes" placeholder="Ex: Pago via Pix no balcão" />
        </div>
      </div>
      <template #footer>
        <Button label="Cancelar" icon="pi pi-times" text @click="showPaymentModal = false" />
        <Button label="Confirmar Recebimento" icon="pi pi-check" :loading="savingPayment" class="p-button-success" @click="submitPayment" />
      </template>
    </Dialog>

    <!-- MODAL NOVA DESPESA -->
    <Dialog header="Lançar Nova Despesa" v-model:visible="showExpenseModal" :modal="true" :style="{ width: '480px' }">
      <div class="p-fluid">
        <div class="mb-3">
          <label class="font-semibold block mb-1">Descrição do Custo *</label>
          <InputText v-model="expenseForm.description" placeholder="Ex: Aluguel do Consultório, Energia, Insumos" required />
        </div>
        <div class="grid">
          <div class="col-6 mb-3">
            <label class="font-semibold block mb-1">Categoria *</label>
            <Dropdown v-model="expenseForm.category" :options="['Aluguel', 'Energia / Água', 'Insumos Médicos', 'Folha / Salários', 'Marketing', 'Softwares', 'Outros']" />
          </div>
          <div class="col-6 mb-3">
            <label class="font-semibold block mb-1">Valor (R$) *</label>
            <InputNumber v-model="expenseForm.amount" mode="currency" currency="BRL" locale="pt-BR" :min="0" />
          </div>
        </div>
        <div class="mb-3">
          <label class="font-semibold block mb-1">Data de Vencimento *</label>
          <input type="date" v-model="expenseForm.dueDate" class="p-inputtext p-component p-2 w-full" required />
        </div>
        <div class="mb-3">
          <label class="font-semibold block mb-1">Observações (opcional)</label>
          <InputText v-model="expenseForm.notes" placeholder="Ex: NF 12345" />
        </div>
      </div>
      <template #footer>
        <Button label="Cancelar" icon="pi pi-times" text @click="showExpenseModal = false" />
        <Button label="Salvar Despesa" icon="pi pi-check" :loading="savingExpense" class="p-button-success" @click="submitExpense" />
      </template>
    </Dialog>

    <!-- MODAL ABRIR CAIXA -->
    <Dialog header="Abertura de Caixa Diário" v-model:visible="showOpenCashModal" :modal="true" :style="{ width: '420px' }">
      <div class="p-fluid">
        <p class="text-sm text-500 mb-3">Informe o valor inicial em dinheiro (troco/fundo de gaveta) para iniciar o expediente.</p>
        <div class="mb-3">
          <label class="font-semibold block mb-1">Fundo de Caixa Inicial (R$)</label>
          <InputNumber v-model="openCashForm.balance" mode="currency" currency="BRL" locale="pt-BR" :min="0" />
        </div>
        <div class="mb-3">
          <label class="font-semibold block mb-1">Observações</label>
          <InputText v-model="openCashForm.notes" placeholder="Ex: Notas de 10 e 20 para troco" />
        </div>
      </div>
      <template #footer>
        <Button label="Cancelar" icon="pi pi-times" text @click="showOpenCashModal = false" />
        <Button label="Abrir Caixa" icon="pi pi-check" :loading="savingCash" class="p-button-success" @click="submitOpenCash" />
      </template>
    </Dialog>

    <!-- MODAL FECHAR CAIXA -->
    <Dialog header="Fechamento de Caixa Diário" v-model:visible="showCloseCashModal" :modal="true" :style="{ width: '420px' }">
      <div class="p-fluid">
        <p class="text-sm text-500 mb-3">Saldo calculado pelo sistema: <strong>{{ formatCurrency(cashSession?.currentBalance) }}</strong>. Realize a contagem física do dinheiro em gaveta:</p>
        <div class="mb-3">
          <label class="font-semibold block mb-1">Valor Físico Conferido (R$)</label>
          <InputNumber v-model="closeCashForm.balance" mode="currency" currency="BRL" locale="pt-BR" :min="0" />
        </div>
        <div class="mb-3">
          <label class="font-semibold block mb-1">Observações de Fechamento</label>
          <InputText v-model="closeCashForm.notes" placeholder="Ex: Caixa batido sem divergências" />
        </div>
      </div>
      <template #footer>
        <Button label="Cancelar" icon="pi pi-times" text @click="showCloseCashModal = false" />
        <Button label="Confirmar Fechamento" icon="pi pi-lock" :loading="savingCash" severity="danger" @click="submitCloseCash" />
      </template>
    </Dialog>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue';
import { useToast } from 'primevue/usetoast';
import Card from 'primevue/card';
import TabView from 'primevue/tabview';
import TabPanel from 'primevue/tabpanel';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import InputNumber from 'primevue/inputnumber';
import Dropdown from 'primevue/dropdown';
import Tag from 'primevue/tag';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import Dialog from 'primevue/dialog';
import Toast from 'primevue/toast';

import { financialService } from '@/api/financial.service';
import { professionalsService } from '@/api/professionals.service';
import { useAuthStore } from '@/stores/auth.store';

const toast = useToast();
const authStore = useAuthStore();

const activeTab = ref(0);
const loading = ref(false);
const startDate = ref('');
const endDate = ref('');
const professionalId = ref(null);
const professionals = ref([]);

const summary = ref({ totalReceived: 0, totalPending: 0, totalRevenue: 0, paidCount: 0, pendingCount: 0, freeReturnCount: 0 });
const transactions = ref([]);
const expenses = ref([]);
const cashSession = ref(null);
const dreData = ref({ grossRevenue: 0, totalCommissions: 0, totalExpenses: 0, netProfit: 0, profitMarginPercentage: 0, revenueByMethod: [], expensesByCategory: [] });

const isSecretary = computed(() => authStore.user?.role === 'Secretaria');

// Modais
const showPaymentModal = ref(false);
const selectedTransaction = ref(null);
const savingPayment = ref(false);
const paymentForm = ref({ amountPaid: 0, paymentMethodId: 1, notes: '' });

const showExpenseModal = ref(false);
const savingExpense = ref(false);
const expenseForm = ref({ description: '', category: 'Aluguel', amount: 0, dueDate: new Date().toISOString().split('T')[0], notes: '' });

const showOpenCashModal = ref(false);
const showCloseCashModal = ref(false);
const savingCash = ref(false);
const openCashForm = ref({ balance: 100, notes: '' });
const closeCashForm = ref({ balance: 0, notes: '' });

const paymentMethods = [
  { label: 'Pix', value: 1 },
  { label: 'Dinheiro', value: 2 },
  { label: 'Cartão de Débito', value: 3 },
  { label: 'Cartão de Crédito', value: 4 },
  { label: 'Boleto Bancário', value: 5 }
];

onMounted(async () => {
  await Promise.all([loadProfessionals(), refreshAll()]);
});

async function refreshAll() {
  await Promise.all([loadFinancial(), loadExpenses(), loadCashSession(), loadDre()]);
}

async function loadProfessionals() {
  try {
    professionals.value = await professionalsService.getAll();
  } catch (err) {
    console.error('Erro ao carregar profissionais', err);
  }
}

async function loadFinancial() {
  loading.value = true;
  try {
    if (isSecretary.value) {
      transactions.value = await financialService.getPendingPayments(startDate.value || null, endDate.value || null);
      summary.value = await financialService.getSummary(startDate.value || null, endDate.value || null);
    } else {
      const report = await financialService.getReport(startDate.value || null, endDate.value || null, professionalId.value || null);
      transactions.value = report.transactions || [];
      summary.value = await financialService.getSummary(startDate.value || null, endDate.value || null);
    }
  } catch (err) {
    console.error('Erro ao carregar dados financeiros', err);
  } finally {
    loading.value = false;
  }
}

async function loadExpenses() {
  try {
    expenses.value = await financialService.getExpenses();
  } catch (err) {
    console.error('Erro ao carregar despesas', err);
  }
}

async function loadCashSession() {
  try {
    cashSession.value = await financialService.getCurrentCashRegister();
  } catch (err) {
    console.error('Erro ao buscar status do caixa', err);
  }
}

async function loadDre() {
  try {
    dreData.value = await financialService.getDre();
  } catch (err) {
    console.error('Erro ao calcular DRE', err);
  }
}

function openPaymentModal(item) {
  selectedTransaction.value = item;
  paymentForm.value = {
    amountPaid: item.remainingBalance,
    paymentMethodId: 1,
    notes: ''
  };
  showPaymentModal.value = true;
}

async function submitPayment() {
  if (!selectedTransaction.value) return;
  savingPayment.value = true;
  try {
    await financialService.registerPayment(selectedTransaction.value.id, {
      amountPaid: paymentForm.value.amountPaid,
      paymentMethodId: paymentForm.value.paymentMethodId,
      notes: paymentForm.value.notes
    });
    showPaymentModal.value = false;
    toast.add({ severity: 'success', summary: 'Sucesso', detail: 'Pagamento registrado com sucesso.', life: 3000 });
    await refreshAll();
  } catch (err) {
    toast.add({ severity: 'error', summary: 'Erro', detail: 'Erro ao registrar pagamento.', life: 3000 });
  } finally {
    savingPayment.value = false;
  }
}

async function submitExpense() {
  if (!expenseForm.value.description || expenseForm.value.amount <= 0) {
    toast.add({ severity: 'warn', summary: 'Atenção', detail: 'Preencha a descrição e o valor.', life: 3000 });
    return;
  }
  savingExpense.value = true;
  try {
    await financialService.createExpense({
      description: expenseForm.value.description,
      category: expenseForm.value.category,
      amount: expenseForm.value.amount,
      dueDate: new Date(expenseForm.value.dueDate).toISOString(),
      notes: expenseForm.value.notes
    });
    showExpenseModal.value = false;
    expenseForm.value = { description: '', category: 'Aluguel', amount: 0, dueDate: new Date().toISOString().split('T')[0], notes: '' };
    toast.add({ severity: 'success', summary: 'Sucesso', detail: 'Despesa cadastrada.', life: 3000 });
    await Promise.all([loadExpenses(), loadDre()]);
  } catch (err) {
    toast.add({ severity: 'error', summary: 'Erro', detail: 'Erro ao cadastrar despesa.', life: 3000 });
  } finally {
    savingExpense.value = false;
  }
}

async function payExpense(item) {
  try {
    await financialService.payExpense(item.id, { paidDate: new Date().toISOString(), paymentMethod: 'Transferência / Pix' });
    toast.add({ severity: 'success', summary: 'Sucesso', detail: 'Despesa marcada como paga.', life: 2500 });
    await Promise.all([loadExpenses(), loadDre()]);
  } catch (err) {
    toast.add({ severity: 'error', summary: 'Erro', detail: 'Não foi possível baixar a despesa.', life: 3000 });
  }
}

async function deleteExpense(id) {
  try {
    await financialService.deleteExpense(id);
    toast.add({ severity: 'info', summary: 'Removida', detail: 'Despesa excluída.', life: 2500 });
    await Promise.all([loadExpenses(), loadDre()]);
  } catch (err) {
    toast.add({ severity: 'error', summary: 'Erro', detail: 'Não foi possível excluir a despesa.', life: 3000 });
  }
}

async function submitOpenCash() {
  savingCash.value = true;
  try {
    await financialService.openCashRegister(openCashForm.value.balance, openCashForm.value.notes);
    showOpenCashModal.value = false;
    toast.add({ severity: 'success', summary: 'Sucesso', detail: 'Caixa diário aberto.', life: 3000 });
    await loadCashSession();
  } catch (err) {
    toast.add({ severity: 'error', summary: 'Erro', detail: err.response?.data?.detail || 'Erro ao abrir caixa.', life: 4000 });
  } finally {
    savingCash.value = false;
  }
}

async function submitCloseCash() {
  savingCash.value = true;
  try {
    await financialService.closeCashRegister(closeCashForm.value.balance, closeCashForm.value.notes);
    showCloseCashModal.value = false;
    toast.add({ severity: 'success', summary: 'Sucesso', detail: 'Caixa diário fechado com sucesso.', life: 3000 });
    await loadCashSession();
  } catch (err) {
    toast.add({ severity: 'error', summary: 'Erro', detail: err.response?.data?.detail || 'Erro ao fechar caixa.', life: 4000 });
  } finally {
    savingCash.value = false;
  }
}

function formatStatus(status) {
  if (status === 1) return 'Pendente';
  if (status === 2) return 'Pago';
  if (status === 3) return 'Cancelado';
  if (status === 4) return 'Retorno Gratuito';
  return 'Desconhecido';
}

function getStatusSeverity(status) {
  if (status === 2) return 'success';
  if (status === 1) return 'warning';
  if (status === 4) return 'info';
  return 'danger';
}

function formatPaymentMethod(methodId) {
  const method = paymentMethods.find(m => m.value === methodId);
  return method ? method.label : '-';
}

function formatCurrency(val) {
  return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(val || 0);
}

function formatDate(date) {
  if (!date) return '-';
  return new Date(date).toLocaleDateString('pt-BR');
}

function formatDateTime(date) {
  if (!date) return '-';
  return new Date(date).toLocaleString('pt-BR');
}
</script>

<style scoped>
.metric-card {
  border-radius: 12px;
}
</style>
