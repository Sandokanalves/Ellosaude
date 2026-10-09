<template>
  <div class="agenda-page">
    <div class="page-header">
      <div>
        <h1 class="text-3xl font-bold text-900 m-0">Agenda da Clínica</h1>
        <p class="text-500 m-0 mt-1">Gerencie consultas, horários e status dos atendimentos</p>
      </div>
      <Button label="+ Novo Agendamento" icon="pi pi-plus" class="p-button-success p-button-raised" @click="handleNewAppointmentClick" />
    </div>

    <Card class="calendar-card p-shadow-3">
      <template #content>
        <FullCalendar :options="calendarOptions" />
      </template>
    </Card>

    <!-- Modal de Novo Agendamento -->
    <Dialog :header="isEditMode ? 'Detalhes do Agendamento' : 'Novo Agendamento'" v-model:visible="showAppointmentModal" :modal="true" :style="{width: '500px'}">
      <div class="p-fluid">
        <div class="p-field mb-3" v-if="!isEditMode">
          <label for="patient" class="font-semibold text-700 mb-1 block">Paciente</label>
          <Dropdown id="patient" v-model="appointment.patientId" :options="patients" optionLabel="name" optionValue="id" placeholder="Selecione um Paciente" filter />
        </div>
        <div class="p-field mb-3" v-else>
          <label class="font-semibold text-700 mb-1 block">Paciente</label>
          <InputText :value="selectedAppointmentInfo?.patientName" readonly />
        </div>

        <div class="p-field mb-3" v-if="!isEditMode">
          <label for="date" class="font-semibold text-700 mb-1 block">Data da Consulta</label>
          <Calendar id="date" v-model="appointment.date" dateFormat="dd/mm/yy" showIcon />
        </div>

        <div class="p-field mb-3" v-if="!isEditMode">
          <label for="start" class="font-semibold text-700 mb-1 block">Hora Início</label>
          <Calendar id="start" v-model="appointment.startTime" :timeOnly="true" showTime hourFormat="24" />
        </div>

        <div class="p-field mb-3" v-if="!isEditMode">
          <label for="end" class="font-semibold text-700 mb-1 block">Hora Fim</label>
          <Calendar id="end" v-model="appointment.endTime" :timeOnly="true" showTime hourFormat="24" />
        </div>

        <div class="p-field mb-3">
          <label for="status" class="font-semibold text-700 mb-1 block">Status do Atendimento</label>
          <Dropdown id="status" v-model="appointment.status" :options="statuses" optionLabel="label" optionValue="value" placeholder="Selecione o Status" />
        </div>
      </div>
      <template #footer>
        <Button label="Cancelar" icon="pi pi-times" class="p-button-text" @click="showAppointmentModal = false"/>
        <Button v-if="isEditMode" label="Atualizar Status" icon="pi pi-check" class="p-button-primary" @click="updateStatus" />
        <Button v-else label="Salvar Agendamento" icon="pi pi-check" class="p-button-success" autofocus @click="saveAppointment" />
      </template>
    </Dialog>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue';
import FullCalendar from '@fullcalendar/vue3';
import dayGridPlugin from '@fullcalendar/daygrid';
import timeGridPlugin from '@fullcalendar/timegrid';
import interactionPlugin from '@fullcalendar/interaction';
import Button from 'primevue/button';
import Dialog from 'primevue/dialog';
import Card from 'primevue/card';
import Dropdown from 'primevue/dropdown';
import Calendar from 'primevue/calendar';
import InputText from 'primevue/inputtext';
import { appointmentsService } from '@/api/appointments.service';
import { patientsService } from '@/api/patients.service';
import { useAuthStore } from '@/stores/auth.store';

const authStore = useAuthStore();
const showAppointmentModal = ref(false);
const isEditMode = ref(false);
const appointment = ref({
  patientId: '',
  date: new Date(),
  startTime: new Date(),
  endTime: new Date(new Date().getTime() + 60 * 60 * 1000),
  status: 0
});
const selectedAppointmentInfo = ref(null);

const patients = ref([]);
const statuses = [
  { label: 'Pendente', value: 0 },
  { label: 'Confirmado', value: 1 },
  { label: 'Realizado', value: 2 },
  { label: 'Cancelado', value: 3 }
];

const calendarOptions = ref({
  plugins: [dayGridPlugin, timeGridPlugin, interactionPlugin],
  initialView: 'timeGridWeek',
  locale: 'pt-br',
  headerToolbar: {
    left: 'prev,next today',
    center: 'title',
    right: 'dayGridMonth,timeGridWeek,timeGridDay'
  },
  editable: true,
  selectable: true,
  selectMirror: true,
  dayMaxEvents: true,
  weekends: true,
  events: [],
  select: handleDateSelect,
  eventClick: handleEventClick,
});

onMounted(async () => {
  await loadPatients();
  await loadAppointments();
});

async function loadPatients() {
  try {
    const list = await patientsService.getAllPatients();
    patients.value = list;
  } catch (err) {
    console.error('Erro ao carregar pacientes', err);
  }
}

async function loadAppointments() {
  try {
    const list = await appointmentsService.getAppointments();
    calendarOptions.value.events = list.map(item => {
      let color = '#E9C46A'; // Pendente (Amarelo)
      if (item.status === 1) color = '#2A9D8F'; // Confirmado (Verde)
      if (item.status === 2) color = '#4CAF50'; // Realizado (Verde Escuro)
      if (item.status === 3) color = '#E76F51'; // Cancelado (Vermelho)

      return {
        id: item.id,
        title: `Consulta - ${item.patientName || 'Paciente'}`,
        start: item.startTime,
        end: item.endTime,
        backgroundColor: color,
        extendedProps: item
      };
    });
  } catch (err) {
    console.error('Erro ao carregar agenda', err);
  }
}

function handleDateSelect(selectInfo) {
  isEditMode.value = false;
  selectedAppointmentInfo.value = null;
  appointment.value = {
    patientId: patients.value[0]?.id || '',
    date: new Date(selectInfo.startStr),
    startTime: new Date(selectInfo.startStr),
    endTime: new Date(selectInfo.endStr),
    status: 0
  };
  showAppointmentModal.value = true;
}

function handleEventClick(clickInfo) {
  isEditMode.value = true;
  selectedAppointmentInfo.value = clickInfo.event.extendedProps;
  appointment.value = {
    id: clickInfo.event.id,
    status: clickInfo.event.extendedProps.status
  };
  showAppointmentModal.value = true;
}

function handleNewAppointmentClick() {
  isEditMode.value = false;
  selectedAppointmentInfo.value = null;
  const now = new Date();
  appointment.value = {
    patientId: patients.value[0]?.id || '',
    date: now,
    startTime: now,
    endTime: new Date(now.getTime() + 60 * 60 * 1000),
    status: 0
  };
  showAppointmentModal.value = true;
}

async function saveAppointment() {
  try {
    const d = new Date(appointment.value.date);
    const start = new Date(appointment.value.startTime);
    const end = new Date(appointment.value.endTime);

    start.setFullYear(d.getFullYear(), d.getMonth(), d.getDate());
    end.setFullYear(d.getFullYear(), d.getMonth(), d.getDate());

    await appointmentsService.createAppointment({
      patientId: appointment.value.patientId,
      professionalId: authStore.user?.id || '00000000-0000-0000-0000-000000000000',
      start: start.toISOString(),
      end: end.toISOString(),
      typeId: 0
    });

    showAppointmentModal.value = false;
    await loadAppointments();
  } catch (err) {
    alert('Erro ao salvar agendamento: ' + (err.message || 'Dados inválidos.'));
  }
}

async function updateStatus() {
  try {
    await appointmentsService.updateStatus(appointment.value.id, appointment.value.status);
    showAppointmentModal.value = false;
    await loadAppointments();
  } catch (err) {
    alert('Erro ao atualizar status: ' + err.message);
  }
}
</script>

<style scoped>
.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1.5rem;
}
.calendar-card {
  border-radius: 12px;
}
</style>
