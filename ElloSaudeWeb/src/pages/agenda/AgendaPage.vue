<template>
  <div class="agenda-page">
    <div class="page-header">
      <div>
        <h1 class="text-3xl font-bold text-900 m-0">Agenda da Clínica</h1>
        <p class="text-500 m-0 mt-1">Gerencie consultas, horários e status dos atendimentos</p>
      </div>
      <Button label="+ Novo Agendamento" icon="pi pi-plus" class="p-button-success p-button-raised" @click="handleNewAppointmentClick" />
    </div>

    <p v-if="pageError" class="error-message" role="alert">{{ pageError }}</p>

    <div class="agenda-filter">
      <label for="professional-filter">Filtrar por profissional</label>
      <Dropdown
        id="professional-filter"
        v-model="calendarProfessionalId"
        :options="professionals"
        optionLabel="name"
        optionValue="id"
        placeholder="Todos os profissionais"
        showClear
        @change="loadAppointments"
      />
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
          <label for="professional" class="font-semibold text-700 mb-1 block">Profissional responsável</label>
          <Dropdown
            id="professional"
            v-model="appointment.professionalId"
            :options="professionals"
            optionLabel="name"
            optionValue="id"
            placeholder="Selecione um profissional"
            @change="loadAvailableSlots"
          />
        </div>

        <div class="p-field mb-3" v-if="!isEditMode">
          <label for="date" class="font-semibold text-700 mb-1 block">Data da Consulta</label>
          <Calendar id="date" v-model="appointment.date" dateFormat="dd/mm/yy" :minDate="minimumDate" showIcon @date-select="loadAvailableSlots" />
        </div>

        <div class="p-field mb-3" v-if="!isEditMode">
          <label for="slot" class="font-semibold text-700 mb-1 block">Horário disponível</label>
          <Dropdown
            id="slot"
            v-model="appointment.selectedSlot"
            :options="availableSlots"
            optionLabel="label"
            optionValue="start"
            placeholder="Selecione um horário"
            :disabled="!availableSlots.length"
          />
          <small v-if="!availableSlots.length" class="text-500">Selecione um profissional e uma data com horários disponíveis.</small>
        </div>

        <div class="p-field mb-3">
          <label for="status" class="font-semibold text-700 mb-1 block">Status do Atendimento</label>
          <Dropdown id="status" v-model="appointment.status" :options="statuses" optionLabel="label" optionValue="value" placeholder="Selecione o Status" />
        </div>
      </div>
      <template #footer>
        <Button label="Cancelar" icon="pi pi-times" class="p-button-text" @click="showAppointmentModal = false"/>
        <Button v-if="isEditMode" label="Atualizar Status" icon="pi pi-check" class="p-button-primary" @click="updateStatus" />
        <Button v-else label="Salvar Agendamento" icon="pi pi-check" class="p-button-success" :disabled="!appointment.selectedSlot" autofocus @click="saveAppointment" />
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
import { professionalsService } from '@/api/professionals.service';
import { useAuthStore } from '@/stores/auth.store';

const authStore = useAuthStore();
const showAppointmentModal = ref(false);
const isEditMode = ref(false);
const appointment = ref({
  patientId: '',
  professionalId: '',
  date: new Date(),
  selectedSlot: '',
  status: 1
});
const selectedAppointmentInfo = ref(null);

const patients = ref([]);
const professionals = ref([]);
const availableSlots = ref([]);
const calendarProfessionalId = ref('');
const pageError = ref('');
const minimumDate = new Date(new Date().setHours(0, 0, 0, 0));
const statuses = [
  { label: 'Pendente', value: 1 },
  { label: 'Confirmado', value: 2 },
  { label: 'Realizado', value: 3 },
  { label: 'Cancelado', value: 4 }
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
  selectable: true,
  selectMirror: true,
  dayMaxEvents: true,
  weekends: true,
  events: [],
  select: handleDateSelect,
  eventClick: handleEventClick,
});

onMounted(async () => {
  await Promise.all([loadPatients(), loadProfessionals()]);
  await loadAppointments();
});

async function loadPatients() {
  try {
    const list = await patientsService.getAllPatients();
    patients.value = list;
  } catch (err) {
    pageError.value = err.response?.data?.detail || 'Não foi possível carregar os pacientes.';
  }
}

async function loadProfessionals() {
  try {
    professionals.value = await professionalsService.getAll();
    const currentProfessional = professionals.value.find(
      professional => professional.email?.toLowerCase() === authStore.user?.email?.toLowerCase()
    );
    calendarProfessionalId.value = currentProfessional?.id || '';
  } catch (err) {
    pageError.value = err.response?.data?.detail || 'Não foi possível carregar os profissionais.';
  }
}

async function loadAppointments() {
  try {
    const list = await appointmentsService.getAppointments();
    calendarOptions.value.events = list
      .filter(item => !calendarProfessionalId.value || item.professionalId === calendarProfessionalId.value)
      .map(item => {
      const colors = { 1: '#E9C46A', 2: '#2A9D8F', 3: '#4CAF50', 4: '#E76F51' };

      return {
        id: item.id,
        title: `${item.professionalName || 'Profissional'} · ${item.patientName || 'Paciente'}`,
        start: item.startTime,
        end: item.endTime,
        backgroundColor: colors[Number(item.status)] || '#6c757d',
        extendedProps: item
      };
    });
  } catch (err) {
    pageError.value = err.response?.data?.detail || 'Não foi possível carregar a agenda.';
  }
}

function handleDateSelect(selectInfo) {
  isEditMode.value = false;
  selectedAppointmentInfo.value = null;
  appointment.value = {
    patientId: patients.value[0]?.id || '',
    professionalId: calendarProfessionalId.value || professionals.value[0]?.id || '',
    date: new Date(selectInfo.start),
    selectedSlot: '',
    status: 1
  };
  availableSlots.value = [];
  showAppointmentModal.value = true;
  loadAvailableSlots();
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
  if (!patients.value.length || !professionals.value.length) {
    pageError.value = 'Cadastre ao menos um paciente e um profissional antes de agendar.';
    return;
  }
  isEditMode.value = false;
  selectedAppointmentInfo.value = null;
  appointment.value = {
    patientId: patients.value[0].id,
    professionalId: calendarProfessionalId.value || professionals.value[0].id,
    date: new Date(),
    selectedSlot: '',
    status: 1
  };
  availableSlots.value = [];
  showAppointmentModal.value = true;
  loadAvailableSlots();
}

async function loadAvailableSlots() {
  appointment.value.selectedSlot = '';
  availableSlots.value = [];
  if (!appointment.value.professionalId || !appointment.value.date) return;

  const date = new Date(appointment.value.date);
  const dateValue = [
    date.getFullYear(),
    String(date.getMonth() + 1).padStart(2, '0'),
    String(date.getDate()).padStart(2, '0')
  ].join('-');

  try {
    const slots = await professionalsService.getAvailableSlots(appointment.value.professionalId, dateValue);
    availableSlots.value = slots
      .filter(slot => slot.isAvailable && new Date(slot.start).getTime() > Date.now())
      .map(slot => ({ ...slot, label: formatTime(slot.start) }));
  } catch (err) {
    pageError.value = err.response?.data?.detail || 'Não foi possível consultar os horários disponíveis.';
  }
}

async function saveAppointment() {
  const selectedSlot = availableSlots.value.find(slot => slot.start === appointment.value.selectedSlot);
  if (!selectedSlot) {
    pageError.value = 'Selecione um horário que ainda esteja disponível.';
    return;
  }

  try {
    await appointmentsService.createAppointment({
      patientId: appointment.value.patientId,
      professionalId: appointment.value.professionalId,
      start: selectedSlot.start,
      end: selectedSlot.end,
      typeId: 1
    });

    showAppointmentModal.value = false;
    pageError.value = '';
    await loadAppointments();
  } catch (err) {
    pageError.value = err.response?.data?.detail || 'Não foi possível salvar o agendamento.';
    await loadAvailableSlots();
  }
}

async function updateStatus() {
  try {
    await appointmentsService.updateStatus(appointment.value.id, appointment.value.status);
    showAppointmentModal.value = false;
    pageError.value = '';
    await loadAppointments();
  } catch (err) {
    pageError.value = err.response?.data?.detail || 'Não foi possível atualizar o status.';
  }
}

function formatTime(value) {
  return new Intl.DateTimeFormat('pt-BR', { timeZone: 'UTC', hour: '2-digit', minute: '2-digit' })
    .format(new Date(value));
}
</script>

<style scoped>
.agenda-filter {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  margin-bottom: 1rem;
}
.agenda-filter :deep(.p-dropdown) {
  min-width: 16rem;
}
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
