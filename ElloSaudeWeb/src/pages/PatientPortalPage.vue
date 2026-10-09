<template>
  <section class="portal-page">
    <header class="page-heading">
      <div>
        <h1>Meu portal</h1>
        <p>Seus agendamentos e receitas liberadas.</p>
      </div>
      <Button label="Atualizar" icon="pi pi-refresh" outlined :loading="loading" @click="load" />
    </header>

    <p v-if="error" class="error-message" role="alert">{{ error }}</p>

    <Card class="section-card">
      <template #title>Agendar uma consulta</template>
      <template #content>
        <div class="booking-form">
          <label>Profissional
            <select v-model="professionalId" @change="loadAvailability">
              <option value="">Selecione</option>
              <option v-for="professional in professionals" :key="professional.id" :value="professional.id">
                {{ professional.name }} — {{ professional.specialty }}
              </option>
            </select>
          </label>
          <label>Data
            <input v-model="bookingDate" type="date" :min="minimumDate" @change="loadAvailability" />
          </label>
          <label>Horário disponível
            <select v-model="selectedSlot" :disabled="!slots.length">
              <option value="">Selecione</option>
              <option v-for="slot in slots" :key="slot.start" :value="slot.start">
                {{ formatTime(slot.start) }}
              </option>
            </select>
          </label>
          <Button label="Confirmar agendamento" icon="pi pi-calendar-plus" :disabled="!selectedSlot" :loading="booking" @click="book" />
        </div>
        <small class="muted">O sistema registra o valor previsto para controle da clínica; nenhum pagamento é processado pelo portal.</small>
      </template>
    </Card>

    <Card class="section-card">
      <template #title>Meus agendamentos</template>
      <template #content>
        <div v-if="!appointments.length && !loading" class="empty-state">Nenhum agendamento encontrado.</div>
        <article v-for="appointment in appointments" :key="appointment.id" class="appointment-row">
          <div>
            <strong>{{ appointment.professionalName }}</strong>
            <p>{{ formatDate(appointment.start) }} às {{ formatTime(appointment.start) }} · {{ statusLabel(appointment.status) }}</p>
          </div>
          <Button
            v-if="canCancel(appointment)"
            label="Cancelar"
            severity="danger"
            text
            @click="cancel(appointment)"
          />
        </article>
      </template>
    </Card>

    <Card class="section-card">
      <template #title>Receitas liberadas</template>
      <template #content>
        <div v-if="!prescriptions.length && !loading" class="empty-state">Nenhuma receita liberada.</div>
        <article v-for="prescription in prescriptions" :key="prescription.id" class="prescription-row">
          <div>
            <strong>{{ prescription.professionalName }} — CRM {{ prescription.crm }}/{{ prescription.crmState }}</strong>
            <p>Emitida em {{ formatDate(prescription.issueDate) }} · {{ prescription.items.length }} medicamento(s)</p>
          </div>
          <Button label="Baixar PDF" icon="pi pi-download" outlined @click="download(prescription)" />
        </article>
      </template>
    </Card>
  </section>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue';
import Button from 'primevue/button';
import Card from 'primevue/card';
import { patientPortalService } from '@/api/patientPortal.service';

const appointments = ref([]);
const professionals = ref([]);
const prescriptions = ref([]);
const slots = ref([]);
const professionalId = ref('');
const selectedSlot = ref('');
const bookingDate = ref('');
const loading = ref(false);
const booking = ref(false);
const error = ref('');
const minimumDate = new Date().toISOString().slice(0, 10);

const selectedSlotDetails = computed(() => slots.value.find(slot => slot.start === selectedSlot.value));

onMounted(async () => {
  bookingDate.value = minimumDate;
  await load();
});

async function load() {
  error.value = '';
  loading.value = true;
  try {
    const [appointmentResult, professionalResult, prescriptionResult] = await Promise.all([
      patientPortalService.getAppointments(),
      patientPortalService.getProfessionals(),
      patientPortalService.getPrescriptions()
    ]);
    appointments.value = appointmentResult;
    professionals.value = professionalResult;
    prescriptions.value = prescriptionResult;
  } catch (err) {
    error.value = err.response?.data?.detail || 'Não foi possível carregar os dados do portal.';
  } finally {
    loading.value = false;
  }
}

async function loadAvailability() {
  selectedSlot.value = '';
  slots.value = [];
  if (!professionalId.value || !bookingDate.value) return;
  error.value = '';
  try {
    slots.value = await patientPortalService.getAvailability(professionalId.value, bookingDate.value);
  } catch (err) {
    error.value = err.response?.data?.detail || 'Não foi possível consultar os horários disponíveis.';
  }
}

async function book() {
  const slot = selectedSlotDetails.value;
  if (!slot) return;
  booking.value = true;
  error.value = '';
  try {
    await patientPortalService.bookAppointment({
      professionalId: professionalId.value,
      start: slot.start,
      end: slot.end
    });
    selectedSlot.value = '';
    await Promise.all([loadAvailability(), load()]);
  } catch (err) {
    error.value = err.response?.data?.detail || 'O horário não está mais disponível. Atualize a lista e tente novamente.';
    await loadAvailability();
  } finally {
    booking.value = false;
  }
}

async function cancel(appointment) {
  if (!window.confirm(`Cancelar a consulta com ${appointment.professionalName}?`)) return;
  error.value = '';
  try {
    await patientPortalService.cancelAppointment(appointment.id);
    await load();
  } catch (err) {
    error.value = err.response?.data?.detail || 'Não foi possível cancelar o agendamento.';
  }
}

function canCancel(appointment) {
  return [1, 2].includes(Number(appointment.status))
    && new Date(appointment.start).getTime() - Date.now() >= 24 * 60 * 60 * 1000;
}

async function download(prescription) {
  error.value = '';
  try {
    const file = await patientPortalService.downloadPrescription(prescription.id);
    const url = URL.createObjectURL(file);
    const link = document.createElement('a');
    link.href = url;
    link.download = `receita-${prescription.id}.pdf`;
    link.click();
    URL.revokeObjectURL(url);
  } catch (err) {
    error.value = err.response?.data?.detail || 'Não foi possível baixar a receita.';
  }
}

function formatDate(value) {
  return new Intl.DateTimeFormat('pt-BR', { timeZone: 'UTC', dateStyle: 'medium' }).format(new Date(value));
}

function formatTime(value) {
  return new Intl.DateTimeFormat('pt-BR', { timeZone: 'UTC', hour: '2-digit', minute: '2-digit' }).format(new Date(value));
}

function statusLabel(status) {
  return ({ 1: 'Pendente', 2: 'Confirmado', 3: 'Realizado', 4: 'Cancelado' })[Number(status)] || 'Status desconhecido';
}
</script>

<style scoped>
.portal-page {
  max-width: 1100px;
  margin: 0 auto;
  display: grid;
  gap: 1.25rem;
}
.page-heading,
.appointment-row,
.prescription-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
}
.page-heading h1,
.page-heading p,
.appointment-row p,
.prescription-row p {
  margin: 0.25rem 0;
}
.booking-form {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
  align-items: end;
  gap: 1rem;
  margin-bottom: 1rem;
}
.booking-form label {
  display: grid;
  gap: 0.4rem;
}
.booking-form select,
.booking-form input {
  min-height: 2.6rem;
  padding: 0.5rem;
  border: 1px solid #bdcdd2;
  border-radius: 6px;
  background: white;
}
.appointment-row,
.prescription-row {
  padding: 0.85rem 0;
  border-bottom: 1px solid #e8eef0;
}
.appointment-row:last-child,
.prescription-row:last-child {
  border-bottom: 0;
}
.empty-state,
.muted {
  color: #667780;
}
.error-message {
  color: #b42318;
  background: #fff1f0;
  padding: 0.75rem;
  border-radius: 6px;
}
@media (max-width: 640px) {
  .page-heading,
  .appointment-row,
  .prescription-row {
    align-items: flex-start;
    flex-direction: column;
  }
}
</style>
