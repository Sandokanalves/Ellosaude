<template>
  <div class="agenda-page">
    <div class="page-header">
      <div>
        <h1 class="text-3xl font-bold text-900 m-0">Agenda da Clínica</h1>
        <p class="text-500 m-0 mt-1">Gerencie consultas, horários e status dos atendimentos</p>
      </div>
      <div class="page-header-actions">
        <Button
          v-if="canManageScheduleBlocks"
          label="Bloquear horário"
          icon="pi pi-ban"
          severity="secondary"
          outlined
          @click="openScheduleBlockDialog"
        />
        <Button label="+ Novo Agendamento" icon="pi pi-plus" class="p-button-success p-button-raised" @click="handleNewAppointmentClick" />
      </div>
    </div>

    <p v-if="pageError" class="error-message" role="alert">{{ pageError }}</p>
    <p v-if="realtimeConnectionError" class="realtime-status" role="status">
      {{ realtimeConnectionError }} A fila continuará atualizando periodicamente.
    </p>
    <p v-if="liveArrival" class="live-arrival" role="status" aria-live="assertive">
      <i class="pi pi-bell" aria-hidden="true"></i>
      Paciente chegou à recepção às {{ formatTime(liveArrival.arrivedAt) }}.
    </p>

    <Card class="waiting-room-card" aria-live="polite">
      <template #title>
        <div class="waiting-room-heading">
          <span><i class="pi pi-bell" aria-hidden="true"></i> Sala de espera</span>
          <span class="waiting-room-count">{{ waitingRoom.length }}</span>
        </div>
      </template>
      <template #content>
        <p v-if="waitingRoomError" class="error-message" role="alert">{{ waitingRoomError }}</p>
        <p v-else-if="!waitingRoom.length" class="text-500 m-0">Nenhum paciente aguardando atendimento.</p>
        <ul v-else class="waiting-room-list">
          <li v-for="item in waitingRoom" :key="item.id" class="waiting-room-entry">
            <div>
              <strong>{{ item.patientName }}</strong>
              <span class="text-500"> · {{ item.professionalName }}</span>
              <small>Chegada às {{ formatTime(item.arrivedAt) }}</small>
            </div>
            <div class="waiting-room-actions">
              <span class="wait-duration">{{ formatWaitDuration(item.arrivedAt) }}</span>
              <Button
                label="Iniciar atendimento"
                icon="pi pi-play"
                size="small"
                @click="startAppointment(item)"
              />
            </div>
          </li>
        </ul>
      </template>
    </Card>

    <Card v-if="authStore.isDoctor" class="notifications-card">
      <template #title>
        <div class="waiting-room-heading">
          <span><i class="pi pi-inbox" aria-hidden="true"></i> Notificações recentes</span>
          <span class="waiting-room-count">{{ unreadNotifications.length }}</span>
        </div>
      </template>
      <template #content>
        <p v-if="notificationsError" class="error-message" role="alert">{{ notificationsError }}</p>
        <p v-else-if="!notifications.length" class="text-500 m-0">Nenhuma notificação recente.</p>
        <ul v-else class="notification-list">
          <li
            v-for="notification in notifications"
            :key="notification.id"
            class="notification-entry"
            :class="{ 'notification-unread': !notification.readAt }"
          >
            <div>
              <strong>{{ notification.title }}</strong>
              <p>{{ notification.message }}</p>
              <small>{{ formatDateTime(notification.occurredAt) }}</small>
            </div>
            <Button
              v-if="!notification.readAt"
              label="Marcar como lida"
              size="small"
              text
              @click="markNotificationRead(notification)"
            />
          </li>
        </ul>
      </template>
    </Card>

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

    <!-- Legenda por Cores de Tipo de Atendimento -->
    <div class="calendar-legend flex flex-wrap gap-3 align-items-center mb-3 px-3 py-2 surface-100 border-round text-sm">
      <span class="font-bold text-700"><i class="pi pi-palette mr-1 text-primary"></i> Categorias:</span>
      <span class="flex align-items-center gap-1"><span class="legend-dot" style="background:#3b82f6"></span> Primeira Consulta</span>
      <span class="flex align-items-center gap-1"><span class="legend-dot" style="background:#10b981"></span> Retorno</span>
      <span class="flex align-items-center gap-1"><span class="legend-dot" style="background:#8b5cf6"></span> Procedimento Estético</span>
      <span class="flex align-items-center gap-1"><span class="legend-dot" style="background:#f59e0b"></span> Encaixe / Walk-In</span>
      <span class="flex align-items-center gap-1"><span class="legend-dot" style="background:#6c757d"></span> Bloqueio de Agenda</span>
    </div>

    <Card class="calendar-card p-shadow-3">
      <template #content>
        <FullCalendar :options="calendarOptions" />
      </template>
    </Card>

    <!-- Modal de Novo Agendamento -->
    <Dialog :header="isEditMode ? 'Detalhes do Agendamento' : 'Novo Agendamento'" v-model:visible="showAppointmentModal" :modal="true" :style="{width: 'min(92vw, 550px)'}">
      <div class="p-fluid">
        <!-- Tipo de Atendimento -->
        <div class="p-field mb-3" v-if="!isEditMode">
          <label for="appointment-type" class="font-semibold text-700 mb-1 block">Tipo de Atendimento</label>
          <Dropdown
            id="appointment-type"
            v-model="appointment.typeId"
            :options="appointmentTypeOptions"
            optionLabel="label"
            optionValue="value"
            placeholder="Selecione o tipo"
            @change="onTypeChange"
          />
        </div>
        <div class="p-field mb-3" v-else>
          <label class="font-semibold text-700 mb-1 block">Tipo de Atendimento</label>
          <div class="flex align-items-center gap-2">
            <Tag :value="getAppointmentTypeLabel(selectedAppointmentInfo?.type)" :severity="getAppointmentTypeSeverity(selectedAppointmentInfo?.type)" />
            <span v-if="selectedAppointmentInfo?.isFreeReturn" class="text-xs text-green-700 font-semibold bg-green-50 px-2 py-1 border-round border-1 border-green-200">Retorno Gratuito</span>
            <span v-if="selectedAppointmentInfo?.packageSessionInfo" class="text-xs text-purple-700 font-semibold bg-purple-50 px-2 py-1 border-round border-1 border-purple-200">{{ selectedAppointmentInfo.packageSessionInfo }}</span>
          </div>
        </div>

        <!-- Se for Procedimento Estético -->
        <div class="p-field mb-3" v-if="!isEditMode && appointment.typeId === 3">
          <label for="procedure-select" class="font-semibold text-700 mb-1 block">Procedimento Estético</label>
          <Dropdown
            id="procedure-select"
            v-model="appointment.procedureId"
            :options="procedures"
            optionLabel="name"
            optionValue="id"
            placeholder="Selecione o procedimento do catálogo"
            @change="onProcedureChange"
          />
        </div>

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

        <!-- Pacote e Sessão -->
        <div class="p-field mb-3" v-if="!isEditMode">
          <label for="package-session" class="font-semibold text-700 mb-1 block">Vínculo de Pacote / Sessão (opcional)</label>
          <InputText
            id="package-session"
            v-model="appointment.packageSessionInfo"
            placeholder="Ex: Sessão 3 de 10"
          />
        </div>

        <!-- Retorno Gratuito (se Retorno) -->
        <div class="p-field mb-3" v-if="!isEditMode && appointment.typeId === 2">
          <div class="flex align-items-center gap-2">
            <Checkbox id="is-free-return" v-model="appointment.isFreeReturn" :binary="true" />
            <label for="is-free-return" class="text-sm font-semibold cursor-pointer">Consulta de Retorno Gratuito (sem cobrança)</label>
          </div>
        </div>

        <!-- Preço Previsto -->
        <div class="p-field mb-3" v-if="!isEditMode && !appointment.isFreeReturn">
          <label for="appointment-price" class="font-semibold text-700 mb-1 block">Valor Previsto (R$)</label>
          <InputNumber
            id="appointment-price"
            v-model="appointment.price"
            mode="currency"
            currency="BRL"
            locale="pt-BR"
            placeholder="0,00"
          />
        </div>

        <div class="p-field mb-3" v-if="!isEditMode">
          <label for="appointment-obs" class="font-semibold text-700 mb-1 block">Observações (opcional)</label>
          <InputText
            id="appointment-obs"
            v-model="appointment.observations"
            placeholder="Queixa inicial, recomendações, etc."
          />
        </div>

        <section v-if="isEditMode" class="status-history" aria-label="Histórico de status">
          <h3>Histórico de status</h3>
          <p v-if="statusHistoryLoading" class="text-500">Carregando histórico...</p>
          <p v-else-if="statusHistoryError" class="error-message" role="alert">{{ statusHistoryError }}</p>
          <p v-else-if="!statusHistory.length" class="text-500">Nenhuma transição registrada.</p>
          <ol v-else>
            <li v-for="transition in statusHistory" :key="transition.id">
              <strong>{{ statusLabel(transition.previousStatus) }} → {{ statusLabel(transition.newStatus) }}</strong>
              <small>{{ formatDateTime(transition.occurredAt) }} · {{ transition.changedByRole }}</small>
            </li>
          </ol>
        </section>

        <div class="p-field mb-3">
          <label for="status" class="font-semibold text-700 mb-1 block">Status do Atendimento</label>
          <Dropdown
            id="status"
            v-model="appointment.status"
            :options="availableStatuses"
            optionLabel="label"
            optionValue="value"
            placeholder="Selecione o Status"
          />
        </div>
      </div>
      <template #footer>
        <Button label="Cancelar" icon="pi pi-times" class="p-button-text" @click="showAppointmentModal = false"/>
        <Button
          v-if="isEditMode && [1, 2].includes(Number(selectedAppointmentInfo?.status))"
          label="WhatsApp"
          icon="pi pi-comment"
          severity="success"
          outlined
          :loading="creatingWhatsAppLink"
          @click="openWhatsAppConfirmation"
        />
        <Button v-if="isEditMode" label="Atualizar Status" icon="pi pi-check" class="p-button-primary" :disabled="appointment.status === selectedAppointmentInfo?.status" @click="updateStatus" />
        <Button v-else label="Salvar Agendamento" icon="pi pi-check" class="p-button-success" :disabled="!appointment.selectedSlot" autofocus @click="saveAppointment" />
      </template>
    </Dialog>

    <Dialog
      header="Bloquear horário da agenda"
      v-model:visible="showScheduleBlockDialog"
      :modal="true"
      :style="{ width: '500px' }"
    >
      <div class="p-fluid">
        <div class="p-field mb-3">
          <label for="block-professional">Profissional</label>
          <Dropdown
            id="block-professional"
            v-model="scheduleBlock.professionalId"
            :options="scheduleBlockProfessionalOptions"
            optionLabel="name"
            optionValue="id"
            placeholder="Selecione um profissional"
          />
        </div>
        <div class="p-field mb-3">
          <label for="block-start">Início</label>
          <Calendar
            id="block-start"
            v-model="scheduleBlock.start"
            showTime
            hourFormat="24"
            showIcon
            dateFormat="dd/mm/yy"
            :minDate="minimumDate"
          />
        </div>
        <div class="p-field mb-3">
          <label for="block-end">Término</label>
          <Calendar
            id="block-end"
            v-model="scheduleBlock.end"
            showTime
            hourFormat="24"
            showIcon
            dateFormat="dd/mm/yy"
            :minDate="scheduleBlock.start || minimumDate"
          />
        </div>
        <div class="p-field mb-3">
          <label for="block-reason">Motivo</label>
          <InputText
            id="block-reason"
            v-model="scheduleBlock.reason"
            maxlength="500"
            placeholder="Ex.: almoço, férias ou ausência"
          />
        </div>
      </div>
      <template #footer>
        <Button label="Cancelar" text @click="showScheduleBlockDialog = false" />
        <Button
          label="Salvar bloqueio"
          icon="pi pi-check"
          :loading="savingScheduleBlock"
          :disabled="!canSaveScheduleBlock"
          @click="saveScheduleBlock"
        />
      </template>
    </Dialog>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue';
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
import InputNumber from 'primevue/inputnumber';
import Checkbox from 'primevue/checkbox';
import Tag from 'primevue/tag';
import { appointmentsService } from '@/api/appointments.service';
import { patientsService } from '@/api/patients.service';
import { professionalsService } from '@/api/professionals.service';
import { notificationsService } from '@/api/notifications.service';
import { clinicsService } from '@/api/clinics.service';
import {
  createWaitingRoomConnection,
  HubConnectionState
} from '@/api/waitingRoomRealtime.service';
import { useAuthStore } from '@/stores/auth.store';

const authStore = useAuthStore();
const showAppointmentModal = ref(false);
const isEditMode = ref(false);
const showScheduleBlockDialog = ref(false);
const savingScheduleBlock = ref(false);
const creatingWhatsAppLink = ref(false);
const statusHistory = ref([]);
const statusHistoryLoading = ref(false);
const statusHistoryError = ref('');
const procedures = ref([]);

const appointmentTypeOptions = [
  { label: 'Primeira Consulta', value: 1 },
  { label: 'Retorno', value: 2 },
  { label: 'Procedimento Estético', value: 3 },
  { label: 'Encaixe (Lista de Espera)', value: 4 }
];

function getAppointmentTypeLabel(type) {
  const map = {
    1: 'Primeira Consulta',
    2: 'Retorno',
    3: 'Procedimento Estético',
    4: 'Encaixe'
  };
  return map[Number(type)] || 'Consulta';
}

function getAppointmentTypeSeverity(type) {
  const map = {
    1: 'info',
    2: 'success',
    3: 'help',
    4: 'warning'
  };
  return map[Number(type)] || 'info';
}

const appointment = ref({
  patientId: '',
  professionalId: '',
  typeId: 1,
  procedureId: null,
  procedureName: '',
  packageSessionInfo: '',
  isWalkIn: false,
  isFreeReturn: false,
  price: 0,
  observations: '',
  colorHex: '#3b82f6',
  date: new Date(),
  selectedSlot: '',
  status: 1
});
const selectedAppointmentInfo = ref(null);

const patients = ref([]);
const professionals = ref([]);
const availableSlots = ref([]);
const calendarProfessionalId = ref('');
const calendarRange = ref({
  start: new Date(new Date().getFullYear(), new Date().getMonth() - 1, 1),
  end: new Date(new Date().getFullYear(), new Date().getMonth() + 2, 1)
});
const scheduleBlock = ref({
  professionalId: '',
  start: new Date(Date.now() + 60 * 60 * 1000),
  end: new Date(Date.now() + 90 * 60 * 1000),
  reason: ''
});
const pageError = ref('');
const waitingRoom = ref([]);
const waitingRoomError = ref('');
const notifications = ref([]);
const notificationsError = ref('');
const unreadNotifications = computed(() =>
  notifications.value.filter(notification => !notification.readAt)
);
const realtimeConnectionError = ref('');
const liveArrival = ref(null);
const now = ref(Date.now());
const minimumDate = new Date(new Date().setHours(0, 0, 0, 0));
const canManageScheduleBlocks = computed(() =>
  ['Admin', 'Profissional'].includes(authStore.user?.role)
);
const scheduleBlockProfessionalOptions = computed(() => {
  if (!authStore.isDoctor) return professionals.value;
  const email = authStore.user?.email?.toLowerCase();
  return professionals.value.filter(professional =>
    professional.email?.toLowerCase() === email
  );
});
const canSaveScheduleBlock = computed(() =>
  !!scheduleBlock.value.professionalId
  && !!scheduleBlock.value.start
  && !!scheduleBlock.value.end
  && scheduleBlock.value.start.getTime() > now.value
  && scheduleBlock.value.end > scheduleBlock.value.start
  && !!scheduleBlock.value.reason.trim()
);
const statusOptions = [
  { label: 'Agendado', value: 1 },
  { label: 'Confirmado', value: 2 },
  { label: 'Finalizado', value: 3 },
  { label: 'Cancelado', value: 4 },
  { label: 'Aguardando na recepção', value: 5 },
  { label: 'Em atendimento', value: 6 },
  { label: 'Faltou', value: 7 }
];
const nextStatusByStatus = {
  1: [2, 4],
  2: [5, 7, 4],
  5: [6],
  6: [3]
};
const availableStatuses = computed(() => {
  if (!isEditMode.value) return statusOptions.filter(status => status.value === 1);
  const currentStatus = Number(selectedAppointmentInfo.value?.status);
  return statusOptions.filter(status =>
    status.value === currentStatus || (nextStatusByStatus[currentStatus] || []).includes(status.value)
  );
});

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
  datesSet: handleCalendarDatesSet,
});

onMounted(async () => {
  await Promise.all([loadPatients(), loadProfessionals(), loadProcedures()]);
  await Promise.all([loadAppointments(), loadWaitingRoom()]);
  if (authStore.isDoctor) await loadNotifications();
  waitingRoomTimer = window.setInterval(loadWaitingRoom, 30000);
  if (authStore.isDoctor) notificationTimer = window.setInterval(loadNotifications, 30000);
  clockTimer = window.setInterval(() => { now.value = Date.now(); }, 10000);
});

async function loadProcedures() {
  try {
    procedures.value = await clinicsService.getProcedures(null, true);
  } catch (err) {
    console.error('Erro ao carregar procedimentos:', err);
  }
}

function onTypeChange() {
  if (appointment.value.typeId === 2) {
    appointment.value.colorHex = '#10b981';
    appointment.value.isFreeReturn = true;
    appointment.value.price = 0;
  } else if (appointment.value.typeId === 3) {
    appointment.value.colorHex = '#8b5cf6';
    appointment.value.isFreeReturn = false;
  } else if (appointment.value.typeId === 4) {
    appointment.value.colorHex = '#f59e0b';
    appointment.value.isFreeReturn = false;
    appointment.value.isWalkIn = true;
  } else {
    appointment.value.colorHex = '#3b82f6';
    appointment.value.isFreeReturn = false;
  }
}

function onProcedureChange() {
  const p = procedures.value.find(item => item.id === appointment.value.procedureId);
  if (p) {
    appointment.value.procedureName = p.name;
    appointment.value.price = p.price;
    if (p.colorHex) appointment.value.colorHex = p.colorHex;
  }
}

let waitingRoomTimer;
let notificationTimer;
let clockTimer;
let hubRetryTimer;
let waitingRoomConnection;
let pageActive = true;
onUnmounted(() => {
  pageActive = false;
  window.clearInterval(waitingRoomTimer);
  window.clearInterval(notificationTimer);
  window.clearInterval(clockTimer);
  window.clearTimeout(hubRetryTimer);
  waitingRoomConnection?.stop();
});

if (authStore.isDoctor) {
  onMounted(connectWaitingRoomHub);
}

async function connectWaitingRoomHub() {
  if (!pageActive || !authStore.token || waitingRoomConnection?.state === HubConnectionState.Connected) return;

  waitingRoomConnection = createWaitingRoomConnection(() => authStore.token);
  waitingRoomConnection.on('PatientArrived', async notification => {
    liveArrival.value = notification;
    now.value = Date.now();
    await Promise.all([loadWaitingRoom(), loadNotifications()]);
  });
  waitingRoomConnection.onreconnecting(() => {
    realtimeConnectionError.value = 'Conexão em tempo real interrompida.';
  });
  waitingRoomConnection.onreconnected(() => {
    realtimeConnectionError.value = '';
    loadWaitingRoom();
    loadNotifications();
  });
  waitingRoomConnection.onclose(error => {
    if (error) realtimeConnectionError.value = 'Conexão em tempo real indisponível.';
    scheduleHubRetry();
  });

  try {
    await waitingRoomConnection.start();
    realtimeConnectionError.value = '';
  } catch {
    realtimeConnectionError.value = 'Não foi possível conectar às notificações em tempo real.';
    scheduleHubRetry();
  }
}

function scheduleHubRetry() {
  if (!pageActive) return;
  window.clearTimeout(hubRetryTimer);
  hubRetryTimer = window.setTimeout(connectWaitingRoomHub, 10000);
}

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
    scheduleBlock.value.professionalId = currentProfessional?.id
      || professionals.value[0]?.id
      || '';
  } catch (err) {
    pageError.value = err.response?.data?.detail || 'Não foi possível carregar os profissionais.';
  }
}

async function loadAppointments() {
  const range = calendarRange.value;
  try {
    const [list, blocks] = await Promise.all([
      appointmentsService.getAppointments(range.start.toISOString(), range.end.toISOString()),
      professionalsService.getScheduleBlocks(
        range.start.toISOString(),
        range.end.toISOString(),
        calendarProfessionalId.value || undefined
      )
    ]);
    const appointmentEvents = list
      .filter(item => !calendarProfessionalId.value || item.professionalId === calendarProfessionalId.value)
      .map(item => {
        const typeColors = {
          1: '#3b82f6', // Primeira Consulta
          2: '#10b981', // Retorno
          3: '#8b5cf6', // Procedimento Estético
          4: '#f59e0b'  // Encaixe
        };

        const eventColor = item.colorHex || typeColors[Number(item.type)] || '#3b82f6';
        const typePrefix = Number(item.type) === 2 ? '[Retorno] ' : Number(item.type) === 3 ? '[Estética] ' : Number(item.type) === 4 ? '[Encaixe] ' : '';
        const sessionSuffix = item.packageSessionInfo ? ` (${item.packageSessionInfo})` : '';

        return {
          id: item.id,
          title: `${typePrefix}${item.professionalName || 'Profissional'} · ${item.patientName || 'Paciente'}${sessionSuffix}`,
          start: item.startTime,
          end: item.endTime,
          backgroundColor: eventColor,
          borderColor: eventColor,
          extendedProps: item
        };
      });
    const blockEvents = blocks
      .filter(block =>
        !calendarProfessionalId.value || block.professionalId === calendarProfessionalId.value
      )
      .map(block => ({
        id: `block-${block.id}`,
        title: `Bloqueado · ${block.reason}`,
        start: block.startDateTime,
        end: block.endDateTime,
        backgroundColor: '#6c757d',
        borderColor: '#495057',
        extendedProps: {
          kind: 'schedule-block',
          blockId: block.id,
          professionalId: block.professionalId,
          professionalName: block.professionalName,
          reason: block.reason
        }
      }));
    calendarOptions.value.events = [...appointmentEvents, ...blockEvents];
  } catch (err) {
    pageError.value = err.response?.data?.detail || 'Não foi possível carregar a agenda.';
  }
}

function handleCalendarDatesSet(dateInfo) {
  calendarRange.value = { start: dateInfo.start, end: dateInfo.end };
  loadAppointments();
}

async function loadWaitingRoom() {
  try {
    waitingRoom.value = await appointmentsService.getWaitingRoom();
    waitingRoomError.value = '';
  } catch (err) {
    waitingRoomError.value = err.response?.data?.detail || 'Não foi possível carregar a sala de espera.';
  }
}

async function loadNotifications() {
  try {
    notifications.value = await notificationsService.getRecent();
    notificationsError.value = '';
  } catch (err) {
    notificationsError.value = err.response?.data?.detail
      || 'Não foi possível carregar as notificações recentes.';
  }
}

async function markNotificationRead(notification) {
  try {
    await notificationsService.markRead(notification.id);
    notification.readAt = new Date().toISOString();
  } catch (err) {
    notificationsError.value = err.response?.data?.detail
      || 'Não foi possível atualizar a notificação.';
  }
}

function handleDateSelect(selectInfo) {
  isEditMode.value = false;
  selectedAppointmentInfo.value = null;
  appointment.value = {
    patientId: patients.value[0]?.id || '',
    professionalId: calendarProfessionalId.value || professionals.value[0]?.id || '',
    typeId: 1,
    procedureId: null,
    procedureName: '',
    packageSessionInfo: '',
    isWalkIn: false,
    isFreeReturn: false,
    price: 0,
    observations: '',
    colorHex: '#3b82f6',
    date: new Date(selectInfo.start),
    selectedSlot: '',
    status: 1
  };
  availableSlots.value = [];
  showAppointmentModal.value = true;
  loadAvailableSlots();
}

function handleEventClick(clickInfo) {
  const eventDetails = clickInfo.event.extendedProps;
  if (eventDetails.kind === 'schedule-block') {
    if (canManageScheduleBlocks.value)
      cancelScheduleBlock(eventDetails);
    return;
  }

  isEditMode.value = true;
  selectedAppointmentInfo.value = eventDetails;
  appointment.value = {
    id: clickInfo.event.id,
    status: clickInfo.event.extendedProps.status
  };
  showAppointmentModal.value = true;
  loadStatusHistory(clickInfo.event.id);
}

function openScheduleBlockDialog() {
  pageError.value = '';
  scheduleBlock.value = {
    professionalId: calendarProfessionalId.value
      || scheduleBlockProfessionalOptions.value[0]?.id
      || '',
    start: new Date(Date.now() + 60 * 60 * 1000),
    end: new Date(Date.now() + 90 * 60 * 1000),
    reason: ''
  };
  showScheduleBlockDialog.value = true;
}

async function saveScheduleBlock() {
  if (!canSaveScheduleBlock.value) return;
  savingScheduleBlock.value = true;
  try {
    await professionalsService.createScheduleBlock(scheduleBlock.value.professionalId, {
      startDateTime: scheduleBlock.value.start.toISOString(),
      endDateTime: scheduleBlock.value.end.toISOString(),
      reason: scheduleBlock.value.reason.trim()
    });
    showScheduleBlockDialog.value = false;
    pageError.value = '';
    await loadAppointments();
  } catch (err) {
    pageError.value = err.response?.data?.detail
      || 'Não foi possível criar o bloqueio. Verifique conflitos com outros bloqueios ou agendamentos.';
  } finally {
    savingScheduleBlock.value = false;
  }
}

async function cancelScheduleBlock(block) {
  if (!window.confirm(`Remover o bloqueio "${block.reason}" da agenda?`))
    return;
  try {
    await professionalsService.cancelScheduleBlock(block.professionalId, block.blockId);
    pageError.value = '';
    await loadAppointments();
  } catch (err) {
    pageError.value = err.response?.data?.detail
      || 'Não foi possível remover o bloqueio da agenda.';
  }
}

async function loadStatusHistory(appointmentId) {
  statusHistory.value = [];
  statusHistoryError.value = '';
  statusHistoryLoading.value = true;
  try {
    statusHistory.value = await appointmentsService.getStatusHistory(appointmentId);
  } catch (err) {
    statusHistoryError.value = err.response?.data?.detail
      || 'Não foi possível carregar o histórico de status.';
  } finally {
    statusHistoryLoading.value = false;
  }
}

function statusLabel(status) {
  return statusOptions.find(option => option.value === Number(status))?.label || 'Status desconhecido';
}

async function startAppointment(item) {
  try {
    await appointmentsService.updateStatus(item.id, 6);
    pageError.value = '';
    await Promise.all([loadWaitingRoom(), loadAppointments()]);
  } catch (err) {
    pageError.value = err.response?.data?.detail || 'Não foi possível iniciar o atendimento.';
  }
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
    typeId: 1,
    procedureId: null,
    procedureName: '',
    packageSessionInfo: '',
    isWalkIn: false,
    isFreeReturn: false,
    price: 0,
    observations: '',
    colorHex: '#3b82f6',
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
      typeId: appointment.value.typeId,
      price: appointment.value.isFreeReturn ? 0 : (appointment.value.price || 0),
      isFreeReturn: appointment.value.isFreeReturn,
      observations: appointment.value.observations || null,
      packageSessionInfo: appointment.value.packageSessionInfo || null,
      isWalkIn: appointment.value.typeId === 4 || appointment.value.isWalkIn,
      procedureId: appointment.value.procedureId || null,
      procedureName: appointment.value.procedureName || null,
      colorHex: appointment.value.colorHex || null
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
    pageError.value = '';
    selectedAppointmentInfo.value.status = Number(appointment.value.status);
    await Promise.all([
      loadAppointments(),
      loadWaitingRoom(),
      loadStatusHistory(appointment.value.id)
    ]);
  } catch (err) {
    pageError.value = err.response?.data?.detail || 'Não foi possível atualizar o status.';
  }
}

async function openWhatsAppConfirmation() {
  const whatsappWindow = window.open('about:blank', '_blank');
  if (!whatsappWindow) {
    pageError.value = 'Permita pop-ups para abrir a conversa do WhatsApp.';
    return;
  }
  whatsappWindow.opener = null;
  creatingWhatsAppLink.value = true;

  try {
    const timeZoneId = Intl.DateTimeFormat().resolvedOptions().timeZone || 'UTC';
    const { url } = await appointmentsService.getWhatsAppConfirmationLink(
      selectedAppointmentInfo.value.id,
      timeZoneId
    );
    whatsappWindow.location.href = url;
    pageError.value = '';
  } catch (err) {
    whatsappWindow.close();
    pageError.value = err.response?.data?.detail
      || 'Não foi possível preparar a confirmação pelo WhatsApp.';
  } finally {
    creatingWhatsAppLink.value = false;
  }
}

function formatTime(value) {
  return new Intl.DateTimeFormat('pt-BR', { timeZone: 'UTC', hour: '2-digit', minute: '2-digit' })
    .format(new Date(value));
}

function formatDateTime(value) {
  return new Intl.DateTimeFormat('pt-BR', {
    dateStyle: 'short',
    timeStyle: 'short',
    timeZone: 'UTC'
  }).format(new Date(value));
}

function formatWaitDuration(arrivedAt) {
  if (!arrivedAt) return 'Tempo indisponível';
  const totalMinutes = Math.max(0, Math.floor((now.value - new Date(arrivedAt).getTime()) / 60000));
  const hours = Math.floor(totalMinutes / 60);
  const minutes = totalMinutes % 60;
  return hours ? `${hours}h ${minutes}min` : `${minutes} min`;
}
</script>

<style scoped>
.waiting-room-card {
  margin-bottom: 1.25rem;
  border-left: 4px solid #e76f51;
}
.notifications-card {
  margin-bottom: 1.25rem;
  border-left: 4px solid #457b9d;
}
.notification-list {
  display: grid;
  gap: 0.5rem;
  margin: 0;
  padding: 0;
  list-style: none;
}
.notification-entry {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  padding: 0.75rem;
  border-top: 1px solid var(--surface-border);
}
.notification-entry p {
  margin: 0.25rem 0;
}
.notification-entry small {
  color: var(--text-color-secondary);
}
.notification-unread {
  background: var(--surface-ground);
}
.realtime-status {
  margin: 0 0 0.75rem;
  color: var(--text-color-secondary);
}
.live-arrival {
  margin: 0 0 0.75rem;
  padding: 0.75rem 1rem;
  border-radius: 6px;
  background: #e8f5e9;
  color: #1b5e20;
  font-weight: 600;
}
.waiting-room-heading,
.waiting-room-entry,
.waiting-room-actions {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
}
.waiting-room-count {
  display: inline-grid;
  min-width: 1.75rem;
  height: 1.75rem;
  place-items: center;
  border-radius: 50%;
  background: #e76f51;
  color: white;
}
.waiting-room-list {
  display: grid;
  gap: 0.75rem;
  margin: 0;
  padding: 0;
  list-style: none;
}
.waiting-room-entry {
  padding: 0.75rem 0;
  border-top: 1px solid var(--surface-border);
}
.waiting-room-entry small {
  display: block;
  margin-top: 0.25rem;
  color: var(--text-color-secondary);
}
.wait-duration {
  color: #b54708;
  font-weight: 700;
  white-space: nowrap;
}
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
  gap: 1rem;
  margin-bottom: 1.5rem;
}
.page-header-actions {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}
.calendar-card {
  border-radius: 12px;
}
.legend-dot {
  width: 12px;
  height: 12px;
  border-radius: 50%;
  display: inline-block;
}
.status-history {
  margin: 1rem 0;
  padding: 0.75rem;
  border: 1px solid var(--surface-border);
  border-radius: 6px;
}
.status-history h3 {
  margin: 0 0 0.5rem;
  font-size: 1rem;
}
.status-history ol {
  display: grid;
  gap: 0.5rem;
  margin: 0;
  padding-left: 1.25rem;
}
.status-history li small {
  display: block;
  color: var(--text-color-secondary);
}
@media (max-width: 640px) {
  .page-header {
    align-items: stretch;
    flex-direction: column;
  }
  .page-header-actions {
    flex-wrap: wrap;
  }
  .waiting-room-entry {
    align-items: flex-start;
    flex-direction: column;
  }
  .waiting-room-actions {
    width: 100%;
  }
}
</style>
