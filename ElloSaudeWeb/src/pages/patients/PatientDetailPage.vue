<template>
  <div class="patient-detail-page">
    <div class="page-header mb-4">
      <div>
        <h1 class="text-3xl font-bold text-900 m-0">Prontuário & Ficha do Paciente</h1>
        <p class="text-500 m-0 mt-1">Histórico médico, evolução e dados cadastrais</p>
      </div>
      <div class="header-actions">
        <Button
          v-if="canCreatePortalAccount && !patient.hasPortalAccount"
          label="Criar acesso ao portal"
          icon="pi pi-user-plus"
          outlined
          @click="createPortalAccount"
        />
        <span v-else-if="patient.hasPortalAccount" class="text-sm text-500">Portal do paciente ativo</span>
        <Button label="Voltar para Lista" icon="pi pi-arrow-left" class="p-button-secondary p-button-outlined" @click="$router.push({ name: 'pacientes' })" />
      </div>
    </div>

    <div class="grid">
      <!-- Coluna da Esquerda: Dados Cadastrais -->
      <div class="col-12 md:col-4">
        <Card class="patient-info-card p-shadow-3">
          <template #content>
            <div class="text-center pb-3 border-bottom-1 surface-border">
              <Avatar icon="pi pi-user" size="xlarge" shape="circle" class="bg-primary text-white mb-2" />
              <h2 class="text-xl font-bold text-900 m-0">{{ patient.name || 'Carregando...' }}</h2>
              <span class="text-sm text-500">Paciente Id: {{ patient.id?.substring(0, 8) }}</span>
            </div>

            <div class="py-3">
              <p class="my-2"><strong>CPF:</strong> {{ patient.cpf || 'Não informado' }}</p>
              <p class="my-2"><strong>E-mail:</strong> {{ patient.email || 'Não informado' }}</p>
              <p class="my-2"><strong>Data de Nasc.:</strong> {{ formatDate(patient.birthDate) }}</p>
            </div>
          </template>
        </Card>
      </div>

      <!-- Coluna da Direita: Abas de Histórico e Prontuário Confidencial -->
      <div class="col-12 md:col-8">
        <TabView>
          <!-- Aba 1: Prontuário Eletrônico (Confidencial - Profissional / Admin) -->
          <TabPanel header="Prontuário Eletrônico (Evoluções)">
            <div v-if="authStore.isDoctor">
              <div class="flex justify-content-between align-items-center mb-4">
                <h3 class="text-lg font-bold text-900 m-0"><i class="pi pi-file-edit mr-2 text-primary"></i> Histórico de Evoluções Médicas</h3>
                <Button label="+ Nova Evolução" icon="pi pi-plus" class="p-button-success p-button-sm" @click="showRecordModal = true" />
              </div>

              <div v-if="medicalRecords.length === 0" class="text-center p-5 surface-100 border-round text-600">
                <i class="pi pi-folder-open text-4xl mb-2 block"></i>
                Nenhuma anotação de prontuário registrada para este paciente.
              </div>

              <div v-else class="timeline">
                <div v-for="record in medicalRecords" :key="record.id" class="record-card surface-card p-4 border-round p-shadow-2 mb-3 border-left-4 border-primary">
                  <div class="flex justify-content-between align-items-center mb-2">
                    <span class="font-bold text-primary"><i class="pi pi-calendar mr-1"></i> {{ formatDate(record.createdAt) }}</span>
                    <small class="text-500">Médico Responsável: Dr. Profissional</small>
                  </div>
                  <h4 class="text-md font-semibold text-900 mt-2 mb-1">Diagnóstico / Hipótese:</h4>
                  <p class="text-700 bg-blue-50 p-2 border-round text-sm m-0 mb-2">{{ record.diagnosis }}</p>
                  <h4 class="text-md font-semibold text-900 mt-2 mb-1">Descrição / Conduta:</h4>
                  <p class="text-700 text-sm m-0 white-space-pre-line">{{ record.description }}</p>
                </div>
              </div>
            </div>

            <!-- Restrição para Secretária -->
            <div v-else class="p-4 bg-orange-50 border-1 border-orange-200 border-round text-orange-900 text-center">
              <i class="pi pi-lock text-3xl mb-2 block text-orange-600"></i>
              <h4 class="m-0 font-bold">Acesso Restrito ao Prontuário</h4>
              <p class="text-sm m-0 mt-1">Conforme os requisitos de confidencialidade (RF10/LGPD), os prontuários médicos são acessíveis apenas pelos Profissionais de Saúde autorizados.</p>
            </div>
          </TabPanel>

          <TabPanel header="Receituário">
            <div v-if="authStore.isDoctor" class="prescription-form">
              <h3>Emitir receita vinculada à consulta</h3>
              <p class="text-500">A emissão é restrita ao profissional responsável por uma consulta realizada.</p>
              <p v-if="prescriptionError" class="error-message" role="alert">{{ prescriptionError }}</p>
              <p v-if="!professionalId" class="error-message" role="alert">
                Não foi possível identificar o vínculo entre sua conta e o profissional cadastrado.
              </p>

              <label for="prescriptionAppointment">Consulta realizada</label>
              <select id="prescriptionAppointment" v-model="newPrescription.appointmentId" required>
                <option value="">Selecione a consulta</option>
                <option v-for="appointment in prescriptionAppointments" :key="appointment.id" :value="appointment.id">
                  {{ formatDate(appointment.startTime) }} — {{ appointment.id.substring(0, 8) }}
                </option>
              </select>
              <p v-if="!prescriptionAppointments.length" class="text-500">
                Não há consultas realizadas por você para este paciente.
              </p>

              <label for="prescriptionNotes">Orientações gerais (opcional)</label>
              <Textarea id="prescriptionNotes" v-model="newPrescription.generalNotes" rows="3" maxlength="1000" />

              <div
                v-for="(item, index) in newPrescription.items"
                :key="index"
                class="prescription-item"
              >
                <div class="prescription-item-heading">
                  <strong>Medicamento {{ index + 1 }}</strong>
                  <Button
                    v-if="newPrescription.items.length > 1"
                    label="Remover"
                    icon="pi pi-trash"
                    severity="danger"
                    text
                    size="small"
                    type="button"
                    @click="removePrescriptionItem(index)"
                  />
                </div>
                <label :for="`medication-${index}`">Medicamento</label>
                <InputText :id="`medication-${index}`" v-model="item.medicationName" maxlength="200" required />
                <label :for="`dosage-${index}`">Dosagem</label>
                <InputText :id="`dosage-${index}`" v-model="item.dosage" maxlength="200" required />
                <label :for="`frequency-${index}`">Frequência / posologia</label>
                <InputText :id="`frequency-${index}`" v-model="item.frequency" maxlength="200" required />
                <label :for="`duration-${index}`">Duração</label>
                <InputText :id="`duration-${index}`" v-model="item.duration" maxlength="100" required />
                <label :for="`route-${index}`">Via de administração</label>
                <InputText :id="`route-${index}`" v-model="item.route" maxlength="100" required />
                <label :for="`instructions-${index}`">Instruções adicionais (opcional)</label>
                <InputText :id="`instructions-${index}`" v-model="item.instructions" maxlength="500" />
              </div>

              <div class="prescription-actions">
                <Button label="Adicionar medicamento" icon="pi pi-plus" outlined type="button" @click="addPrescriptionItem" />
                <Button
                  label="Emitir e baixar PDF"
                  icon="pi pi-file-pdf"
                  :loading="savingPrescription"
                  :disabled="!professionalId || !prescriptionAppointments.length"
                  @click="createPrescription"
                />
              </div>
            </div>
            <div v-else class="p-4 bg-orange-50 border-1 border-orange-200 border-round text-orange-900 text-center">
              A emissão de receitas é restrita ao profissional de saúde responsável pela consulta.
            </div>
          </TabPanel>
        </TabView>
      </div>
    </div>

    <!-- Modal Adicionar Anotação de Prontuário -->
    <Dialog header="Nova Anotação de Prontuário" v-model:visible="showRecordModal" :modal="true" :style="{width: '500px'}">
      <div class="p-fluid">
        <div class="p-field mb-3">
          <label for="recordAppointment" class="font-semibold text-700 mb-1 block">Consulta realizada</label>
          <select id="recordAppointment" v-model="newRecord.appointmentId" required>
            <option value="">Selecione a consulta</option>
            <option v-for="appointment in completedAppointments" :key="appointment.id" :value="appointment.id">
              {{ formatDate(appointment.startTime) }} — {{ appointment.id.substring(0, 8) }}
            </option>
          </select>
        </div>
        <div class="p-field mb-3">
          <label for="diagnosis" class="font-semibold text-700 mb-1 block">Diagnóstico / CID / Hipótese</label>
          <InputText id="diagnosis" v-model="newRecord.diagnosis" placeholder="Ex: CID10 J00 - Nasofaringite aguda" />
        </div>
        <div class="p-field mb-3">
          <label for="description" class="font-semibold text-700 mb-1 block">Evolução / Prescrição / Observações</label>
          <Textarea id="description" v-model="newRecord.description" rows="5" placeholder="Descreva os sintomas, achados do exame físico e conduta médica..." />
        </div>
      </div>
      <template #footer>
        <Button label="Cancelar" icon="pi pi-times" class="p-button-text" @click="showRecordModal = false" />
        <Button label="Salvar no Prontuário" icon="pi pi-check" class="p-button-success" :loading="savingRecord" @click="saveMedicalRecord" />
      </template>
    </Dialog>

    <Dialog
      header="Acesso ao portal do paciente"
      v-model:visible="showPortalCredentials"
      :modal="true"
      :style="{ width: 'min(94vw, 520px)' }"
      @hide="clearPortalCredentials"
    >
      <p>Entregue esta senha temporária ao paciente por um canal externo seguro. Ela será exibida somente agora e deverá ser trocada no primeiro acesso.</p>
      <div v-if="portalCredentials" class="credential-box">
        <strong>{{ portalCredentials.email }}</strong>
        <code>{{ portalCredentials.temporaryPassword }}</code>
      </div>
      <template #footer>
        <Button label="Copiar senha" icon="pi pi-copy" outlined @click="copyTemporaryPassword" />
        <Button label="Fechar" @click="showPortalCredentials = false" />
      </template>
    </Dialog>
  </div>
</template>

<script setup>
import { computed, ref, onMounted } from 'vue';
import { useRoute } from 'vue-router';
import Button from 'primevue/button';
import Card from 'primevue/card';
import Avatar from 'primevue/avatar';
import TabView from 'primevue/tabview';
import TabPanel from 'primevue/tabpanel';
import Dialog from 'primevue/dialog';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import { patientsService } from '@/api/patients.service';
import { medicalRecordsService } from '@/api/medicalRecords.service';
import { useAuthStore } from '@/stores/auth.store';
import { appointmentsService } from '@/api/appointments.service';
import { professionalsService } from '@/api/professionals.service';
import { prescriptionsService } from '@/api/prescriptions.service';

const route = useRoute();
const authStore = useAuthStore();
const patientId = route.params.id;

const patient = ref({});
const medicalRecords = ref([]);
const showRecordModal = ref(false);
const savingRecord = ref(false);
const completedAppointments = ref([]);
const showPortalCredentials = ref(false);
const portalCredentials = ref(null);
const professionalId = ref('');
const prescriptionError = ref('');
const savingPrescription = ref(false);
const canCreatePortalAccount = ['Admin', 'Secretaria'].includes(authStore.user?.role);
const prescriptionAppointments = computed(() => completedAppointments.value
  .filter(appointment => appointment.professionalId === professionalId.value));

function newPrescriptionItem() {
  return {
    medicationName: '',
    dosage: '',
    frequency: '',
    duration: '',
    route: '',
    instructions: ''
  };
}

const newRecord = ref({
  appointmentId: '',
  diagnosis: '',
  description: ''
});
const newPrescription = ref({
  appointmentId: '',
  generalNotes: '',
  items: [newPrescriptionItem()]
});

onMounted(async () => {
  await loadPatient();
  if (authStore.isDoctor) {
    await Promise.all([loadMedicalRecords(), loadCompletedAppointments(), loadCurrentProfessional()]);
  }
});

async function loadPatient() {
  try {
    patient.value = await patientsService.getPatientById(patientId);
  } catch (err) {
    console.error('Erro ao buscar paciente', err);
  }
}

async function loadCompletedAppointments() {
  try {
    const appointments = await appointmentsService.getAppointments();
    completedAppointments.value = appointments.filter(
      appointment => appointment.patientId === patientId && Number(appointment.status) === 3
    );
  } catch (err) {
    console.error('Erro ao buscar consultas realizadas', err);
  }
}

async function loadCurrentProfessional() {
  try {
    const professionals = await professionalsService.getAll();
    professionalId.value = professionals.find(
      professional => professional.email?.toLowerCase() === authStore.user?.email?.toLowerCase()
    )?.id || '';
  } catch (err) {
    prescriptionError.value = err.response?.data?.detail || 'Não foi possível validar o profissional autenticado.';
  }
}

async function loadMedicalRecords() {
  try {
    medicalRecords.value = await medicalRecordsService.getRecordsByPatient(patientId);
  } catch (err) {
    console.error('Erro ao buscar prontuários', err);
  }
}

function addPrescriptionItem() {
  newPrescription.value.items.push(newPrescriptionItem());
}

function removePrescriptionItem(index) {
  if (newPrescription.value.items.length > 1)
    newPrescription.value.items.splice(index, 1);
}

async function createPrescription() {
  if (!professionalId.value || !newPrescription.value.appointmentId
    || !newPrescription.value.items.every(item =>
      item.medicationName.trim()
      && item.dosage.trim()
      && item.frequency.trim()
      && item.duration.trim()
      && item.route.trim())) {
    prescriptionError.value = 'Selecione uma consulta e preencha todos os campos obrigatórios de cada medicamento.';
    return;
  }

  savingPrescription.value = true;
  prescriptionError.value = '';
  try {
    const result = await prescriptionsService.create({
      appointmentId: newPrescription.value.appointmentId,
      professionalId: professionalId.value,
      patientId,
      generalNotes: newPrescription.value.generalNotes.trim() || null,
      items: newPrescription.value.items.map(item => ({
        medicationName: item.medicationName.trim(),
        dosage: item.dosage.trim(),
        frequency: item.frequency.trim(),
        duration: item.duration.trim(),
        route: item.route.trim(),
        instructions: item.instructions.trim() || null
      }))
    });
    const pdf = await prescriptionsService.downloadPdf(result.id);
    const url = URL.createObjectURL(pdf);
    const link = document.createElement('a');
    link.href = url;
    link.download = `receita-${result.id}.pdf`;
    link.click();
    window.setTimeout(() => URL.revokeObjectURL(url), 1000);
    newPrescription.value = {
      appointmentId: '',
      generalNotes: '',
      items: [newPrescriptionItem()]
    };
  } catch (err) {
    prescriptionError.value = err.response?.data?.detail || 'Não foi possível emitir ou baixar a receita.';
  } finally {
    savingPrescription.value = false;
  }
}

async function saveMedicalRecord() {
  if (!newRecord.value.appointmentId || !newRecord.value.diagnosis || !newRecord.value.description) {
    alert('Selecione a consulta realizada e preencha o diagnóstico e a descrição.');
    return;
  }
  savingRecord.value = true;
  try {
    await medicalRecordsService.createRecord({
      patientId: patientId,
      appointmentId: newRecord.value.appointmentId,
      diagnosis: newRecord.value.diagnosis,
      description: newRecord.value.description
    });
    newRecord.value = { appointmentId: '', diagnosis: '', description: '' };
    showRecordModal.value = false;
    await loadMedicalRecords();
  } catch (err) {
    alert('Erro ao salvar prontuário: ' + err.message);
  } finally {
    savingRecord.value = false;
  }
}

async function createPortalAccount() {
  try {
    portalCredentials.value = await patientsService.createPortalAccount(patientId);
    showPortalCredentials.value = true;
    patient.value.hasPortalAccount = true;
  } catch (err) {
    alert(err.response?.data?.detail || 'Não foi possível criar o acesso do paciente.');
    await loadPatient();
  }
}

async function copyTemporaryPassword() {
  if (portalCredentials.value?.temporaryPassword) {
    await navigator.clipboard.writeText(portalCredentials.value.temporaryPassword);
  }
}

function clearPortalCredentials() {
  portalCredentials.value = null;
}

function formatDate(dateStr) {
  if (!dateStr) return 'N/I';
  const d = new Date(dateStr);
  return d.toLocaleDateString('pt-BR');
}
</script>

<style scoped>
.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}
.header-actions {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}
.credential-box {
  display: grid;
  gap: 0.5rem;
  padding: 1rem;
  background: #f2f6f7;
  border-radius: 8px;
}
.credential-box code {
  overflow-wrap: anywhere;
}
.patient-info-card {
  border-radius: 12px;
}
.record-card {
  transition: transform 0.2s ease;
}
.record-card:hover {
  transform: translateY(-2px);
}
.prescription-form {
  display: grid;
  gap: 0.65rem;
}
.prescription-form h3,
.prescription-form p {
  margin: 0;
}
.prescription-item {
  display: grid;
  gap: 0.5rem;
  padding: 1rem;
  border: 1px solid var(--surface-border);
  border-radius: 8px;
}
.prescription-item-heading,
.prescription-actions {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
}
.prescription-actions {
  justify-content: flex-start;
  flex-wrap: wrap;
}
</style>
