<template>
  <div>
    <div class="page-header">
      <div>
        <h1 class="text-3xl font-bold text-900 m-0">Gestão de Pacientes</h1>
        <p class="text-500 m-0 mt-1">Cadastre, localize e gerencie o histórico de pacientes</p>
      </div>
      <Button label="+ Novo Paciente" icon="pi pi-user-plus" class="p-button-success p-button-raised" @click="openNewPatientModal" />
    </div>

    <Card class="table-card p-shadow-3">
      <template #content>
        <DataTable :value="patients" :paginator="true" :rows="10" :filters="filters" :loading="loading">
          <template #header>
            <div class="table-header">
              <span class="p-input-icon-left">
                <i class="pi pi-search" />
                <InputText placeholder="Buscar por nome ou CPF..." v-model="filters['global'].value" />
              </span>
            </div>
          </template>
          <template #empty>Nenhum paciente cadastrado até o momento.</template>
          
          <Column field="name" header="Nome" sortable></Column>
          <Column field="cpf" header="CPF"></Column>
          <Column field="email" header="E-mail"></Column>
          <Column header="Ações" style="width: 12rem">
            <template #body="slotProps">
              <Button icon="pi pi-eye" class="p-button-rounded p-button-info mr-2" v-tooltip="'Ver Prontuário'" @click="viewPatient(slotProps.data)" />
              <Button icon="pi pi-pencil" class="p-button-rounded p-button-warning mr-2" v-tooltip="'Editar'" @click="editPatient(slotProps.data)" />
              <Button icon="pi pi-trash" class="p-button-rounded p-button-danger" v-tooltip="'Excluir'" @click="deletePatient(slotProps.data)" />
            </template>
          </Column>
        </DataTable>
      </template>
    </Card>
    
    <PatientFormModal 
      :visible="isPatientModalVisible" 
      :patientData="selectedPatient"
      @close="closePatientModal" 
      @save="handleSavePatient" 
    />
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import Button from 'primevue/button';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import InputText from 'primevue/inputtext';
import Card from 'primevue/card';
import PatientFormModal from '@/components/PatientFormModal.vue';
import { patientsService } from '@/api/patients.service';

const router = useRouter();
const patients = ref([]);
const loading = ref(false);
const isPatientModalVisible = ref(false);
const selectedPatient = ref(null);

const filters = ref({ 'global': { value: null, matchMode: 'contains' } });

onMounted(async () => {
  await loadPatients();
});

async function loadPatients() {
  loading.value = true;
  try {
    patients.value = await patientsService.getAllPatients();
  } catch (err) {
    console.error('Erro ao carregar pacientes', err);
  } finally {
    loading.value = false;
  }
}

const openNewPatientModal = () => {
  selectedPatient.value = null;
  isPatientModalVisible.value = true;
};

const editPatient = (patient) => {
  selectedPatient.value = { ...patient };
  isPatientModalVisible.value = true;
};

const closePatientModal = () => {
  isPatientModalVisible.value = false;
};

const handleSavePatient = async (patientData) => {
  try {
    if (patientData.id) {
      await patientsService.updatePatient(patientData.id, patientData);
    } else {
      await patientsService.createPatient(patientData);
    }
    closePatientModal();
    await loadPatients();
  } catch (err) {
    alert('Erro ao salvar paciente: ' + (err.message || 'Dados inválidos.'));
  }
};

const deletePatient = async (patient) => {
  if (confirm(`Tem certeza que deseja excluir o paciente ${patient.name}?`)) {
    try {
      await patientsService.deletePatient(patient.id);
      await loadPatients();
    } catch (err) {
      alert('Erro ao excluir paciente: ' + err.message);
    }
  }
};

const viewPatient = (patient) => {
  router.push({ name: 'paciente-detalhe', params: { id: patient.id } });
};
</script>

<style scoped>
.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1.5rem;
}
.table-header {
  display: flex;
  justify-content: flex-end;
}
.table-card {
  border-radius: 12px;
}
</style>
