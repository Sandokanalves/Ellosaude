<template>
  <div class="settings-page">
    <Toast />
    
    <div class="page-header mb-4 flex justify-content-between align-items-center">
      <div>
        <h1 class="text-3xl font-bold text-900 m-0">Configurações da Clínica</h1>
        <p class="text-500 m-0 mt-1">Gestão de dados cadastrais, regras de negócio, tabela de preços e equipe de colaboradores.</p>
      </div>
    </div>

    <TabView>
      <!-- Aba 1: Dados da Clínica & Marca -->
      <TabPanel header="Dados da Clínica & Marca">
        <Card class="p-shadow-2">
          <template #title>
            <div class="flex align-items-center gap-2">
              <i class="pi pi-building text-primary text-xl"></i>
              <span>Identidade Visual e Informações do Tenant</span>
            </div>
          </template>
          <template #content>
            <div class="grid p-fluid">
              <div class="col-12 md:col-8">
                <div class="grid">
                  <div class="col-12 md:col-6 mb-3">
                    <label class="font-semibold block mb-1">Razão Social</label>
                    <InputText v-model="settings.name" placeholder="Ex: Clínica Ello Saúde Ltda" />
                  </div>
                  <div class="col-12 md:col-6 mb-3">
                    <label class="font-semibold block mb-1">Nome Fantasia</label>
                    <InputText v-model="settings.nameFantasy" placeholder="Ex: Ello Saúde" />
                  </div>
                  <div class="col-12 md:col-6 mb-3">
                    <label class="font-semibold block mb-1">CNPJ</label>
                    <InputText v-model="settings.cnpj" placeholder="00.000.000/0001-00" />
                  </div>
                  <div class="col-12 md:col-6 mb-3">
                    <label class="font-semibold block mb-1">Telefone / WhatsApp</label>
                    <InputText v-model="settings.phone" placeholder="(81) 99999-9999" />
                  </div>
                  <div class="col-12 md:col-6 mb-3">
                    <label class="font-semibold block mb-1">E-mail de Contato</label>
                    <InputText v-model="settings.email" placeholder="contato@ellosaude.com.br" />
                  </div>
                  <div class="col-12 md:col-6 mb-3">
                    <label class="font-semibold block mb-1">Endereço Completo</label>
                    <InputText v-model="settings.address" placeholder="Av. Principal, 100 - Sala 402" />
                  </div>
                  <div class="col-12 md:col-6 mb-3">
                    <label class="font-semibold block mb-1">Cidade</label>
                    <InputText v-model="settings.city" placeholder="Recife" />
                  </div>
                  <div class="col-12 md:col-6 mb-3">
                    <label class="font-semibold block mb-1">Estado (UF)</label>
                    <InputText v-model="settings.state" placeholder="PE" maxlength="2" />
                  </div>
                </div>

                <div class="border-top-1 surface-border pt-3 mt-3">
                  <h4 class="text-md font-bold text-800 m-0 mb-3"><i class="pi pi-cog mr-2 text-primary"></i>Políticas Clínicas e Comerciais</h4>
                  <div class="grid">
                    <div class="col-12 md:col-6 mb-3">
                      <label class="font-semibold block mb-1">Prazo Máximo de Retorno Gratuito (Dias)</label>
                      <InputNumber v-model="settings.maxReturnDays" :min="0" :max="180" suffix=" dias" />
                      <small class="text-500">Consultas de retorno após esse prazo serão cobradas normalmente.</small>
                    </div>
                    <div class="col-12 md:col-6 mb-3">
                      <label class="font-semibold block mb-1">Repasse / Comissão Médica Padrão (%)</label>
                      <InputNumber v-model="settings.defaultCommissionPercentage" :min="0" :max="100" suffix=" %" :minFractionDigits="1" />
                      <small class="text-500">Percentual padrão sobre consultas para repasse aos profissionais.</small>
                    </div>
                  </div>
                </div>
              </div>

              <!-- Coluna do Logotipo -->
              <div class="col-12 md:col-4 flex flex-column align-items-center justify-content-start border-left-1 surface-border pl-4">
                <label class="font-semibold mb-2">Logotipo da Clínica</label>
                <div class="logo-preview-box surface-100 border-2 border-dashed surface-border border-round flex flex-column align-items-center justify-content-center p-4 mb-3 w-full" style="min-height: 180px;">
                  <img v-if="settings.logoUrl" :src="settings.logoUrl" alt="Logo Clínica" class="max-w-full max-h-10rem border-round" />
                  <div v-else class="text-center text-500">
                    <i class="pi pi-image text-4xl mb-2"></i>
                    <p class="m-0 text-sm">Nenhum logotipo configurado</p>
                  </div>
                </div>
                <div class="w-full mb-3">
                  <label class="text-sm font-semibold block mb-1">URL da Imagem / Logotipo</label>
                  <InputText v-model="settings.logoUrl" placeholder="https://exemplo.com/logo.png" class="w-full" />
                </div>
                <small class="text-500 text-center">O logotipo será incluído automaticamente nos cabeçalhos de receitas, atestados e termos gerados em PDF.</small>
              </div>
            </div>

            <div class="flex justify-content-end mt-4">
              <Button label="Salvar Configurações" icon="pi pi-check" :loading="savingSettings" class="p-button-success" @click="saveSettings" />
            </div>
          </template>
        </Card>
      </TabPanel>

      <!-- Aba 2: Tabela de Procedimentos & Preços -->
      <TabPanel header="Tabela de Preços & Procedimentos">
        <Card class="p-shadow-2">
          <template #title>
            <div class="flex justify-content-between align-items-center">
              <div class="flex align-items-center gap-2">
                <i class="pi pi-list text-primary text-xl"></i>
                <span>Procedimentos e Catálogo de Atendimentos</span>
              </div>
              <Button label="+ Novo Procedimento" icon="pi pi-plus" class="p-button-primary p-button-sm" @click="openProcedureModal()" />
            </div>
          </template>
          <template #content>
            <DataTable :value="procedures" :loading="loadingProcedures" responsiveLayout="scroll" class="p-datatable-sm" emptyMessage="Nenhum procedimento cadastrado.">
              <Column field="name" header="Procedimento" sortable>
                <template #body="{ data }">
                  <div class="flex align-items-center gap-2">
                    <span class="color-badge" :style="{ backgroundColor: data.colorHex || '#4f46e5' }"></span>
                    <strong class="text-900">{{ data.name }}</strong>
                  </div>
                </template>
              </Column>
              <Column field="specialty" header="Especialidade" sortable></Column>
              <Column field="durationMinutes" header="Duração" sortable>
                <template #body="{ data }">
                  <span>{{ data.durationMinutes }} min</span>
                </template>
              </Column>
              <Column field="price" header="Valor (R$)" sortable>
                <template #body="{ data }">
                  <strong class="text-green-600">{{ formatCurrency(data.price) }}</strong>
                </template>
              </Column>
              <Column field="isActive" header="Status">
                <template #body="{ data }">
                  <Tag :value="data.isActive ? 'Ativo' : 'Inativo'" :severity="data.isActive ? 'success' : 'danger'" />
                </template>
              </Column>
              <Column header="Ações" style="width: 140px;">
                <template #body="{ data }">
                  <div class="flex gap-2">
                    <Button icon="pi pi-pencil" text rounded severity="info" size="small" @click="openProcedureModal(data)" title="Editar" />
                    <Button :icon="data.isActive ? 'pi pi-ban' : 'pi pi-check'" text rounded :severity="data.isActive ? 'danger' : 'success'" size="small" @click="toggleProcedure(data)" :title="data.isActive ? 'Desativar' : 'Ativar'" />
                  </div>
                </template>
              </Column>
            </DataTable>
          </template>
        </Card>
      </TabPanel>

      <!-- Aba 3: Equipe & Colaboradores -->
      <TabPanel header="Equipe & Colaboradores">
        <Card class="p-shadow-2">
          <template #title>
            <div class="flex justify-content-between align-items-center">
              <div class="flex align-items-center gap-2">
                <i class="pi pi-users text-primary text-xl"></i>
                <span>Usuários e Colaboradores da Clínica</span>
              </div>
              <Button label="+ Novo Colaborador" icon="pi pi-user-plus" class="p-button-primary p-button-sm" @click="showUserModal = true" />
            </div>
          </template>
          <template #content>
            <DataTable :value="staffUsers" :loading="loadingStaff" responsiveLayout="scroll" class="p-datatable-sm" emptyMessage="Nenhum colaborador encontrado.">
              <Column field="fullName" header="Nome" sortable>
                <template #body="{ data }">
                  <div class="flex align-items-center gap-2">
                    <Avatar icon="pi pi-user" shape="circle" class="bg-primary-100 text-primary" />
                    <div>
                      <strong class="text-900 block">{{ data.fullName || data.email.split('@')[0] }}</strong>
                      <small class="text-500">{{ data.email }}</small>
                    </div>
                  </div>
                </template>
              </Column>
              <Column field="role" header="Papel / Perfil" sortable>
                <template #body="{ data }">
                  <Tag :value="getRoleLabel(data.role)" :severity="getRoleSeverity(data.role)" />
                </template>
              </Column>
              <Column field="createdAt" header="Cadastrado em" sortable>
                <template #body="{ data }">
                  <span>{{ formatDate(data.createdAt) }}</span>
                </template>
              </Column>
              <Column field="isActive" header="Status">
                <template #body="{ data }">
                  <Tag :value="data.isActive ? 'Ativo' : 'Inativo'" :severity="data.isActive ? 'success' : 'danger'" />
                </template>
              </Column>
              <Column header="Ações" style="width: 120px;">
                <template #body="{ data }">
                  <Button
                    :icon="data.isActive ? 'pi pi-user-minus' : 'pi pi-user-check'"
                    text
                    rounded
                    :severity="data.isActive ? 'danger' : 'success'"
                    size="small"
                    @click="toggleUser(data)"
                    :title="data.isActive ? 'Desativar acesso' : 'Reativar acesso'"
                  />
                </template>
              </Column>
            </DataTable>
          </template>
        </Card>
      </TabPanel>
    </TabView>

    <!-- Modal Procedimento -->
    <Dialog v-model:visible="showProcedureModal" :header="editingProcedureId ? 'Editar Procedimento' : 'Novo Procedimento'" :modal="true" :style="{ width: '500px' }">
      <div class="p-fluid">
        <div class="mb-3">
          <label class="font-semibold block mb-1">Nome do Procedimento</label>
          <InputText v-model="procedureForm.name" placeholder="Ex: Consulta Dermatológica, Toxina Botulínica" required />
        </div>
        <div class="mb-3">
          <label class="font-semibold block mb-1">Especialidade</label>
          <Dropdown v-model="procedureForm.specialty" :options="specialtyOptions" placeholder="Selecione a especialidade" editable />
        </div>
        <div class="grid">
          <div class="col-6 mb-3">
            <label class="font-semibold block mb-1">Preço (R$)</label>
            <InputNumber v-model="procedureForm.price" mode="currency" currency="BRL" locale="pt-BR" :min="0" />
          </div>
          <div class="col-6 mb-3">
            <label class="font-semibold block mb-1">Duração (min)</label>
            <InputNumber v-model="procedureForm.durationMinutes" :min="5" :step="5" suffix=" min" />
          </div>
        </div>
        <div class="mb-3">
          <label class="font-semibold block mb-1">Cor na Grade da Agenda</label>
          <div class="flex gap-2 align-items-center">
            <input type="color" v-model="procedureForm.colorHex" class="color-picker-input" />
            <InputText v-model="procedureForm.colorHex" placeholder="#4f46e5" class="w-8rem" />
            <div class="flex gap-1 ml-2">
              <span v-for="c in presetColors" :key="c" :style="{ backgroundColor: c }" class="preset-color-dot" @click="procedureForm.colorHex = c"></span>
            </div>
          </div>
        </div>
      </div>
      <template #footer>
        <Button label="Cancelar" icon="pi pi-times" text @click="showProcedureModal = false" />
        <Button label="Salvar Procedimento" icon="pi pi-check" :loading="savingProcedure" class="p-button-success" @click="saveProcedure" />
      </template>
    </Dialog>

    <!-- Modal Novo Colaborador -->
    <Dialog v-model:visible="showUserModal" header="Cadastrar Novo Colaborador" :modal="true" :style="{ width: '500px' }">
      <div class="p-fluid">
        <div class="mb-3">
          <label class="font-semibold block mb-1">Nome Completo</label>
          <InputText v-model="userForm.fullName" placeholder="Ex: Maria Santos" required />
        </div>
        <div class="mb-3">
          <label class="font-semibold block mb-1">E-mail Profissional</label>
          <InputText v-model="userForm.email" type="email" placeholder="colaborador@ellosaude.com.br" required />
        </div>
        <div class="mb-3">
          <label class="font-semibold block mb-1">Perfil de Acesso (Papel)</label>
          <Dropdown v-model="userForm.role" :options="roleOptions" optionLabel="label" optionValue="value" placeholder="Selecione o perfil" />
          <small class="text-500 block mt-1">
            <strong>Admin:</strong> Acesso total. <strong>Profissional:</strong> Acesso a atendimentos e prontuários. <strong>Secretaria:</strong> Acesso a agenda e recepção.
          </small>
        </div>
        <div class="mb-3">
          <label class="font-semibold block mb-1">Senha Provisória</label>
          <InputText v-model="userForm.password" type="password" placeholder="Mínimo 6 caracteres" required />
        </div>
      </div>
      <template #footer>
        <Button label="Cancelar" icon="pi pi-times" text @click="showUserModal = false" />
        <Button label="Criar Colaborador" icon="pi pi-check" :loading="savingUser" class="p-button-success" @click="createUser" />
      </template>
    </Dialog>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue';
import { useToast } from 'primevue/usetoast';
import Card from 'primevue/card';
import TabView from 'primevue/tabview';
import TabPanel from 'primevue/tabpanel';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import InputNumber from 'primevue/inputnumber';
import Dropdown from 'primevue/dropdown';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import Tag from 'primevue/tag';
import Avatar from 'primevue/avatar';
import Dialog from 'primevue/dialog';
import Toast from 'primevue/toast';
import { clinicsService } from '@/api/clinics.service';
import { usersService } from '@/api/users.service';

const toast = useToast();

const settings = ref({
  name: '',
  nameFantasy: '',
  cnpj: '',
  phone: '',
  email: '',
  address: '',
  city: '',
  state: '',
  logoUrl: '',
  maxReturnDays: 30,
  defaultCommissionPercentage: 50.0
});

const savingSettings = ref(false);

const procedures = ref([]);
const loadingProcedures = ref(false);
const showProcedureModal = ref(false);
const editingProcedureId = ref(null);
const savingProcedure = ref(false);

const procedureForm = ref({
  name: '',
  specialty: '',
  price: 150.0,
  durationMinutes: 30,
  colorHex: '#4f46e5'
});

const presetColors = ['#3b82f6', '#10b981', '#8b5cf6', '#f59e0b', '#ec4899', '#06b6d4', '#6366f1'];

const specialtyOptions = [
  'Clínica Geral',
  'Dermatologia',
  'Estética Avançada',
  'Fisioterapia',
  'Nutrição',
  'Cardiologia',
  'Ginecologia',
  'Ortopedia',
  'Psicologia'
];

const staffUsers = ref([]);
const loadingStaff = ref(false);
const showUserModal = ref(false);
const savingUser = ref(false);

const userForm = ref({
  fullName: '',
  email: '',
  role: 'Secretaria',
  password: ''
});

const roleOptions = [
  { label: 'Administrador (Admin)', value: 'Admin' },
  { label: 'Médico / Profissional de Saúde', value: 'Profissional' },
  { label: 'Recepção / Secretária', value: 'Secretaria' }
];

onMounted(() => {
  loadSettings();
  loadProcedures();
  loadStaff();
});

async function loadSettings() {
  try {
    const data = await clinicsService.getSettings();
    if (data) {
      settings.value = { ...data };
    }
  } catch (err) {
    console.error('Erro ao carregar configurações', err);
  }
}

async function saveSettings() {
  savingSettings.value = true;
  try {
    await clinicsService.updateSettings(settings.value);
    toast.add({ severity: 'success', summary: 'Sucesso', detail: 'Configurações da clínica atualizadas com sucesso.', life: 3000 });
  } catch (err) {
    toast.add({ severity: 'error', summary: 'Erro', detail: err.response?.data?.detail || 'Erro ao salvar configurações.', life: 4000 });
  } finally {
    savingSettings.value = false;
  }
}

async function loadProcedures() {
  loadingProcedures.value = true;
  try {
    procedures.value = await clinicsService.getProcedures();
  } catch (err) {
    console.error('Erro ao carregar procedimentos', err);
  } finally {
    loadingProcedures.value = false;
  }
}

function openProcedureModal(proc = null) {
  if (proc) {
    editingProcedureId.value = proc.id;
    procedureForm.value = {
      name: proc.name,
      specialty: proc.specialty,
      price: proc.price,
      durationMinutes: proc.durationMinutes,
      colorHex: proc.colorHex || '#4f46e5'
    };
  } else {
    editingProcedureId.value = null;
    procedureForm.value = {
      name: '',
      specialty: 'Clínica Geral',
      price: 150.0,
      durationMinutes: 30,
      colorHex: '#4f46e5'
    };
  }
  showProcedureModal.value = true;
}

async function saveProcedure() {
  if (!procedureForm.value.name || !procedureForm.value.specialty) {
    toast.add({ severity: 'warn', summary: 'Atenção', detail: 'Preencha o nome e a especialidade do procedimento.', life: 3000 });
    return;
  }

  savingProcedure.value = true;
  try {
    if (editingProcedureId.value) {
      await clinicsService.updateProcedure(editingProcedureId.value, {
        id: editingProcedureId.value,
        ...procedureForm.value
      });
      toast.add({ severity: 'success', summary: 'Sucesso', detail: 'Procedimento atualizado.', life: 3000 });
    } else {
      await clinicsService.createProcedure(procedureForm.value);
      toast.add({ severity: 'success', summary: 'Sucesso', detail: 'Procedimento cadastrado com sucesso.', life: 3000 });
    }
    showProcedureModal.value = false;
    await loadProcedures();
  } catch (err) {
    toast.add({ severity: 'error', summary: 'Erro', detail: 'Erro ao salvar procedimento.', life: 4000 });
  } finally {
    savingProcedure.value = false;
  }
}

async function toggleProcedure(proc) {
  try {
    await clinicsService.toggleProcedureStatus(proc.id);
    await loadProcedures();
    toast.add({ severity: 'info', summary: 'Atualizado', detail: `Procedimento ${proc.name} ${proc.isActive ? 'desativado' : 'ativado'}.`, life: 2500 });
  } catch (err) {
    toast.add({ severity: 'error', summary: 'Erro', detail: 'Não foi possível alterar o status do procedimento.', life: 3000 });
  }
}

async function loadStaff() {
  loadingStaff.value = true;
  try {
    staffUsers.value = await usersService.getStaff();
  } catch (err) {
    console.error('Erro ao carregar colaboradores', err);
  } finally {
    loadingStaff.value = false;
  }
}

async function createUser() {
  if (!userForm.value.fullName || !userForm.value.email || !userForm.value.password) {
    toast.add({ severity: 'warn', summary: 'Atenção', detail: 'Preencha todos os campos obrigatórios.', life: 3000 });
    return;
  }

  savingUser.value = true;
  try {
    await usersService.createStaff(userForm.value);
    toast.add({ severity: 'success', summary: 'Sucesso', detail: 'Colaborador cadastrado com sucesso.', life: 3000 });
    showUserModal.value = false;
    userForm.value = { fullName: '', email: '', role: 'Secretaria', password: '' };
    await loadStaff();
  } catch (err) {
    toast.add({ severity: 'error', summary: 'Erro', detail: err.response?.data?.detail || 'Erro ao criar colaborador.', life: 4000 });
  } finally {
    savingUser.value = false;
  }
}

async function toggleUser(user) {
  try {
    await usersService.toggleStaffStatus(user.id);
    await loadStaff();
    toast.add({ severity: 'info', summary: 'Atualizado', detail: `Acesso do colaborador ${user.isActive ? 'bloqueado' : 'reativado'}.`, life: 2500 });
  } catch (err) {
    toast.add({ severity: 'error', summary: 'Erro', detail: 'Não foi possível alterar o status do usuário.', life: 3000 });
  }
}

function getRoleLabel(role) {
  if (role === 'Admin') return 'Administrador';
  if (role === 'Profissional') return 'Médico / Profissional';
  if (role === 'Secretaria') return 'Recepção / Secretária';
  return role;
}

function getRoleSeverity(role) {
  if (role === 'Admin') return 'danger';
  if (role === 'Profissional') return 'info';
  if (role === 'Secretaria') return 'warning';
  return 'secondary';
}

function formatCurrency(val) {
  return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(val || 0);
}

function formatDate(date) {
  if (!date) return '-';
  return new Date(date).toLocaleDateString('pt-BR');
}
</script>

<style scoped>
.color-badge {
  display: inline-block;
  width: 14px;
  height: 14px;
  border-radius: 50%;
}
.color-picker-input {
  width: 40px;
  height: 38px;
  border: 1px solid #ced4da;
  border-radius: 6px;
  cursor: pointer;
  padding: 2px;
}
.preset-color-dot {
  display: inline-block;
  width: 22px;
  height: 22px;
  border-radius: 50%;
  cursor: pointer;
  border: 2px solid white;
  box-shadow: 0 0 2px rgba(0,0,0,0.3);
  transition: transform 0.15s ease;
}
.preset-color-dot:hover {
  transform: scale(1.2);
}
</style>
