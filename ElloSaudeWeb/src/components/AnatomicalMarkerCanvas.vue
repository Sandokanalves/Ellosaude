<template>
  <div class="anatomical-marker-component">
    <div class="flex justify-content-between align-items-center mb-3">
      <div class="flex gap-2">
        <Button
          :label="'Rosto / Facial'"
          icon="pi pi-user"
          :severity="selectedTarget === 'Face' ? 'primary' : 'secondary'"
          :outlined="selectedTarget !== 'Face'"
          size="small"
          @click="changeTarget('Face')"
        />
        <Button
          :label="'Corpo (Frente)'"
          icon="pi pi-id-card"
          :severity="selectedTarget === 'BodyFront' ? 'primary' : 'secondary'"
          :outlined="selectedTarget !== 'BodyFront'"
          size="small"
          @click="changeTarget('BodyFront')"
        />
        <Button
          :label="'Corpo (Costas)'"
          icon="pi pi-align-justify"
          :severity="selectedTarget === 'BodyBack' ? 'primary' : 'secondary'"
          :outlined="selectedTarget !== 'BodyBack'"
          size="small"
          @click="changeTarget('BodyBack')"
        />
      </div>
      <div class="text-xs text-500">
        <i class="pi pi-info-circle mr-1"></i>Clique sobre o mapa visual para inserir um marcador.
      </div>
    </div>

    <div class="grid">
      <!-- Área do Mapa SVG Interativo -->
      <div class="col-12 md:col-7 flex justify-content-center">
        <div class="canvas-wrapper surface-card border-1 surface-border border-round p-3 relative select-none">
          <!-- Face SVG -->
          <svg
            v-if="selectedTarget === 'Face'"
            viewBox="0 0 400 480"
            class="anatomical-svg"
            @click="handleSvgClick"
          >
            <!-- Contorno Face -->
            <path d="M120,60 Q200,30 280,60 Q340,120 330,240 Q310,380 200,440 Q90,380 70,240 Q60,120 120,60 Z" fill="#f8fafc" stroke="#94a3b8" stroke-width="3" />
            <!-- Sobrancelhas -->
            <path d="M110,150 Q150,135 180,150" fill="none" stroke="#64748b" stroke-width="4" stroke-linecap="round" />
            <path d="M220,150 Q250,135 290,150" fill="none" stroke="#64748b" stroke-width="4" stroke-linecap="round" />
            <!-- Olhos -->
            <ellipse cx="145" cy="175" rx="28" ry="14" fill="#e2e8f0" stroke="#64748b" stroke-width="2" />
            <circle cx="145" cy="175" r="7" fill="#475569" />
            <ellipse cx="255" cy="175" rx="28" ry="14" fill="#e2e8f0" stroke="#64748b" stroke-width="2" />
            <circle cx="255" cy="175" r="7" fill="#475569" />
            <!-- Nariz -->
            <path d="M200,165 L195,260 L215,260 Z" fill="#e2e8f0" stroke="#94a3b8" stroke-width="2" />
            <!-- Boca / Lábios -->
            <path d="M150,320 Q200,305 250,320 Q200,355 150,320 Z" fill="#fed7aa" stroke="#fb923c" stroke-width="2" />
            <line x1="150" y1="320" x2="250" y2="320" stroke="#ea580c" stroke-width="1.5" />
            <!-- Maçãs do Rosto -->
            <ellipse cx="115" cy="245" rx="20" ry="12" fill="#fee2e2" opacity="0.6" />
            <ellipse cx="285" cy="245" rx="20" ry="12" fill="#fee2e2" opacity="0.6" />
            <!-- Linhas guia estéticas (Glabela, Fronte, Mandíbula) -->
            <circle cx="200" cy="145" r="3" fill="#cbd5e1" />
            <text x="200" y="470" text-anchor="middle" font-size="12" fill="#94a3b8">Mapa Facial / Estética</text>
          </svg>

          <!-- Corpo Frente SVG -->
          <svg
            v-else-if="selectedTarget === 'BodyFront'"
            viewBox="0 0 400 600"
            class="anatomical-svg"
            @click="handleSvgClick"
          >
            <!-- Cabeça -->
            <ellipse cx="200" cy="50" rx="35" ry="42" fill="#f8fafc" stroke="#94a3b8" stroke-width="3" />
            <!-- Pescoço -->
            <path d="M185,90 L185,120 L215,120 L215,90" fill="#f8fafc" stroke="#94a3b8" stroke-width="2" />
            <!-- Tronco e Ombros -->
            <path d="M140,120 Q200,110 260,120 L285,145 L250,300 L240,360 L160,360 L150,300 L115,145 Z" fill="#f8fafc" stroke="#94a3b8" stroke-width="3" />
            <!-- Braço Esquerdo -->
            <path d="M115,145 L85,260 L75,340 L90,345 L105,265 L135,160 Z" fill="#f8fafc" stroke="#94a3b8" stroke-width="2" />
            <!-- Braço Direito -->
            <path d="M285,145 L315,260 L325,340 L310,345 L295,265 L265,160 Z" fill="#f8fafc" stroke="#94a3b8" stroke-width="2" />
            <!-- Perna Esquerda -->
            <path d="M165,360 L160,470 L155,570 L180,575 L190,470 L195,360 Z" fill="#f8fafc" stroke="#94a3b8" stroke-width="2" />
            <!-- Perna Direita -->
            <path d="M235,360 L240,470 L245,570 L220,575 L210,470 L205,360 Z" fill="#f8fafc" stroke="#94a3b8" stroke-width="2" />
            <!-- Linhas do Peitoral e Abdômen -->
            <line x1="165" y1="180" x2="235" y2="180" stroke="#cbd5e1" stroke-width="2" />
            <circle cx="200" cy="270" r="3" fill="#cbd5e1" />
            <text x="200" y="595" text-anchor="middle" font-size="12" fill="#94a3b8">Anatomia Corporal (Frente)</text>
          </svg>

          <!-- Corpo Costas SVG -->
          <svg
            v-else
            viewBox="0 0 400 600"
            class="anatomical-svg"
            @click="handleSvgClick"
          >
            <!-- Cabeça -->
            <ellipse cx="200" cy="50" rx="35" ry="42" fill="#f8fafc" stroke="#94a3b8" stroke-width="3" />
            <!-- Pescoço -->
            <path d="M185,90 L185,120 L215,120 L215,90" fill="#f8fafc" stroke="#94a3b8" stroke-width="2" />
            <!-- Costas e Escápulas -->
            <path d="M140,120 Q200,110 260,120 L285,145 L250,300 L240,360 L160,360 L150,300 L115,145 Z" fill="#f8fafc" stroke="#94a3b8" stroke-width="3" />
            <!-- Coluna Vertebral -->
            <line x1="200" y1="120" x2="200" y2="350" stroke="#cbd5e1" stroke-width="3" stroke-dasharray="4" />
            <!-- Escápulas -->
            <path d="M165,150 Q150,190 170,220" fill="none" stroke="#94a3b8" stroke-width="2" />
            <path d="M235,150 Q250,190 230,220" fill="none" stroke="#94a3b8" stroke-width="2" />
            <!-- Braços -->
            <path d="M115,145 L85,260 L75,340 L90,345 L105,265 L135,160 Z" fill="#f8fafc" stroke="#94a3b8" stroke-width="2" />
            <path d="M285,145 L315,260 L325,340 L310,345 L295,265 L265,160 Z" fill="#f8fafc" stroke="#94a3b8" stroke-width="2" />
            <!-- Pernas -->
            <path d="M165,360 L160,470 L155,570 L180,575 L190,470 L195,360 Z" fill="#f8fafc" stroke="#94a3b8" stroke-width="2" />
            <path d="M235,360 L240,470 L245,570 L220,575 L210,470 L205,360 Z" fill="#f8fafc" stroke="#94a3b8" stroke-width="2" />
            <text x="200" y="595" text-anchor="middle" font-size="12" fill="#94a3b8">Coluna & Dor Postural (Costas)</text>
          </svg>

          <!-- Marcadores Plotados -->
          <div
            v-for="marker in currentMarkers"
            :key="marker.id"
            class="plotted-marker flex align-items-center justify-content-center"
            :style="{ left: `${marker.x}%`, top: `${marker.y}%`, backgroundColor: getMarkerColor(marker.type) }"
            :title="`${marker.label} (${marker.intensityOrDose || ''})`"
            @click.stop="openMarkerDetails(marker)"
          >
            <span class="marker-dot"></span>
          </div>
        </div>
      </div>

      <!-- Tabela Lateral de Marcadores Atuais -->
      <div class="col-12 md:col-5">
        <div class="surface-card border-1 surface-border border-round p-3 h-full">
          <div class="flex justify-content-between align-items-center mb-3">
            <h4 class="font-bold text-900 m-0 text-sm">
              <i class="pi pi-map-marker text-primary mr-1"></i> Marcadores Cadastrados ({{ currentMarkers.length }})
            </h4>
          </div>

          <div v-if="currentMarkers.length === 0" class="text-center p-4 surface-50 border-round text-500 text-sm">
            Nenhum ponto marcado nesta região. Clique no mapa ao lado para registrar aplicação ou lesão.
          </div>

          <div v-else class="marker-list overflow-y-auto" style="max-height: 480px;">
            <div
              v-for="m in currentMarkers"
              :key="m.id"
              class="marker-item p-2 mb-2 border-round surface-50 border-left-3 flex justify-content-between align-items-center"
              :style="{ borderLeftColor: getMarkerColor(m.type) }"
            >
              <div>
                <strong class="text-sm text-900 block">{{ m.label }}</strong>
                <small class="text-600 block">
                  <span class="font-semibold">{{ m.type }}</span>
                  <span v-if="m.intensityOrDose"> • {{ m.intensityOrDose }}</span>
                </small>
                <small v-if="m.notes" class="text-500 italic block">{{ m.notes }}</small>
              </div>
              <Button icon="pi pi-trash" text rounded severity="danger" size="small" @click="removeMarker(m.id)" title="Excluir ponto" />
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Modal Novo Marcador -->
    <Dialog v-model:visible="showModal" header="Adicionar Marcador Anatômico" :modal="true" :style="{ width: '420px' }">
      <div class="p-fluid">
        <div class="mb-3">
          <label class="font-semibold block mb-1">Tipo de Aplicação / Ocorrência</label>
          <Dropdown v-model="form.type" :options="markerTypes" placeholder="Selecione o tipo" />
        </div>
        <div class="mb-3">
          <label class="font-semibold block mb-1">Identificação / Ponto Anatômico</label>
          <InputText v-model="form.label" placeholder="Ex: Glabela, Ponto Gatilho Trapézio, Sulco Nasogeniano" required />
        </div>
        <div class="mb-3">
          <label class="font-semibold block mb-1">Dose / Volume / Intensidade (opcional)</label>
          <InputText v-model="form.intensityOrDose" placeholder="Ex: 4U, 0.5ml, Grau 8/10" />
        </div>
        <div class="mb-3">
          <label class="font-semibold block mb-1">Observações da Aplicação</label>
          <InputText v-model="form.notes" placeholder="Ex: Aplicação profunda supraperióstea" />
        </div>
      </div>
      <template #footer>
        <Button label="Cancelar" icon="pi pi-times" text @click="showModal = false" />
        <Button label="Salvar Ponto" icon="pi pi-check" :loading="saving" class="p-button-success" @click="saveMarker" />
      </template>
    </Dialog>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue';
import Button from 'primevue/button';
import Dialog from 'primevue/dialog';
import InputText from 'primevue/inputtext';
import Dropdown from 'primevue/dropdown';
import { patientsService } from '@/api/patients.service';

const props = defineProps({
  patientId: {
    type: String,
    required: true
  }
});

const selectedTarget = ref('Face');
const markers = ref([]);
const showModal = ref(false);
const saving = ref(false);

const pendingCoord = ref({ x: 50, y: 50 });

const form = ref({
  type: 'Toxina',
  label: '',
  intensityOrDose: '',
  notes: ''
});

const markerTypes = [
  'Toxina Botulínica',
  'Preenchimento (AH)',
  'Bioestimulador',
  'Ponto de Dor / Gatilho',
  'Lesão Dermatológica',
  'Quelóide / Cicatriz',
  'Outro'
];

const currentMarkers = computed(() => {
  return markers.value.filter(m => m.target === selectedTarget.value);
});

onMounted(() => {
  loadMarkers();
});

async function loadMarkers() {
  try {
    markers.value = await patientsService.getMarkers(props.patientId);
  } catch (err) {
    console.error('Erro ao carregar marcadores', err);
  }
}

function changeTarget(target) {
  selectedTarget.value = target;
}

function handleSvgClick(event) {
  const rect = event.currentTarget.getBoundingClientRect();
  const x = ((event.clientX - rect.left) / rect.width) * 100;
  const y = ((event.clientY - rect.top) / rect.height) * 100;

  pendingCoord.value = {
    x: Math.round(x * 10) / 10,
    y: Math.round(y * 10) / 10
  };

  form.value = {
    type: selectedTarget.value === 'Face' ? 'Toxina Botulínica' : 'Ponto de Dor / Gatilho',
    label: '',
    intensityOrDose: '',
    notes: ''
  };

  showModal.value = true;
}

async function saveMarker() {
  if (!form.value.label) return;

  saving.value = true;
  try {
    await patientsService.saveMarker(props.patientId, {
      target: selectedTarget.value,
      x: pendingCoord.value.x,
      y: pendingCoord.value.y,
      type: form.value.type,
      label: form.value.label,
      intensityOrDose: form.value.intensityOrDose,
      notes: form.value.notes
    });
    showModal.value = false;
    await loadMarkers();
  } catch (err) {
    console.error('Erro ao salvar marcador', err);
  } finally {
    saving.value = false;
  }
}

async function removeMarker(id) {
  try {
    await patientsService.deleteMarker(props.patientId, id);
    await loadMarkers();
  } catch (err) {
    console.error('Erro ao remover marcador', err);
  }
}

function getMarkerColor(type) {
  if (type.includes('Toxina')) return '#8b5cf6'; // Roxo
  if (type.includes('Preenchimento')) return '#ec4899'; // Rosa
  if (type.includes('Bioestimulador')) return '#3b82f6'; // Azul
  if (type.includes('Dor')) return '#ef4444'; // Vermelho
  if (type.includes('Lesão')) return '#f59e0b'; // Laranja
  return '#10b981'; // Verde
}
</script>

<style scoped>
.canvas-wrapper {
  width: 100%;
  max-width: 420px;
  min-height: 480px;
  display: flex;
  justify-content: center;
  align-items: center;
  background: #ffffff;
}

.anatomical-svg {
  width: 100%;
  height: auto;
  max-height: 480px;
  cursor: crosshair;
}

.plotted-marker {
  position: absolute;
  width: 18px;
  height: 18px;
  border-radius: 50%;
  transform: translate(-50%, -50%);
  cursor: pointer;
  box-shadow: 0 0 6px rgba(0, 0, 0, 0.4);
  border: 2px solid #ffffff;
  z-index: 10;
  transition: transform 0.15s ease;
}

.plotted-marker:hover {
  transform: translate(-50%, -50%) scale(1.4);
}

.marker-dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: white;
}
</style>
