<template>
  <div class="patient-detail-page">
    <div class="page-header mb-4">
      <div>
        <h1 class="text-3xl font-bold text-900 m-0">Prontuário & Ficha do Paciente</h1>
        <p class="text-500 m-0 mt-1">Histórico médico, evolução e dados cadastrais</p>
      </div>
      <Button label="Voltar para Lista" icon="pi pi-arrow-left" class="p-button-secondary p-button-outlined" @click="$router.push({ name: 'pacientes' })" />
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
        </TabView>
      </div>
    </div>

    <!-- Modal Adicionar Anotação de Prontuário -->
    <Dialog header="Nova Anotação de Prontuário" v-model:visible="showRecordModal" :modal="true" :style="{width: '500px'}">
      <div class="p-fluid">
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
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue';
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

const route = useRoute();
const authStore = useAuthStore();
const patientId = route.params.id;

const patient = ref({});
const medicalRecords = ref([]);
const showRecordModal = ref(false);
const savingRecord = ref(false);

const newRecord = ref({
  diagnosis: '',
  description: ''
});

onMounted(async () => {
  await loadPatient();
  if (authStore.isDoctor) {
    await loadMedicalRecords();
  }
});

async function loadPatient() {
  try {
    patient.value = await patientsService.getPatientById(patientId);
  } catch (err) {
    console.error('Erro ao buscar paciente', err);
  }
}

async function loadMedicalRecords() {
  try {
    medicalRecords.value = await medicalRecordsService.getRecordsByPatient(patientId);
  } catch (err) {
    console.error('Erro ao buscar prontuários', err);
  }
}

async function saveMedicalRecord() {
  if (!newRecord.value.diagnosis || !newRecord.value.description) {
    alert('Preencha o diagnóstico e a descrição.');
    return;
  }
  savingRecord.value = true;
  try {
    await medicalRecordsService.createRecord({
      patientId: patientId,
      diagnosis: newRecord.value.diagnosis,
      description: newRecord.value.description
    });
    newRecord.value = { diagnosis: '', description: '' };
    showRecordModal.value = false;
    await loadMedicalRecords();
  } catch (err) {
    alert('Erro ao salvar prontuário: ' + err.message);
  } finally {
    savingRecord.value = false;
  }
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
.patient-info-card {
  border-radius: 12px;
}
.record-card {
  transition: transform 0.2s ease;
}
.record-card:hover {
  transform: translateY(-2px);
}
</style>
