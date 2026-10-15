<template>
  <div class="patient-detail-page">
    <Toast />
    
    <div class="page-header mb-4">
      <div>
        <h1 class="text-3xl font-bold text-900 m-0">Prontuário & Ficha do Paciente</h1>
        <p class="text-500 m-0 mt-1">Visão integral, histórico clínico por abas e gestão do atendimento.</p>
      </div>
      <div class="header-actions">
        <Button
          v-if="canCreatePortalAccount && !patient.hasPortalAccount"
          label="Criar acesso ao portal"
          icon="pi pi-user-plus"
          outlined
          @click="createPortalAccount"
        />
        <span v-else-if="patient.hasPortalAccount" class="text-sm font-semibold text-green-600 bg-green-50 px-3 py-1 border-round border-1 border-green-200">
          <i class="pi pi-check-circle mr-1"></i>Portal Ativo
        </span>
        <Button label="Voltar para Lista" icon="pi pi-arrow-left" class="p-button-secondary p-button-outlined" @click="$router.push({ name: 'pacientes' })" />
      </div>
    </div>

    <div class="grid">
      <!-- Coluna da Esquerda: Resumo Cadastral Fixo -->
      <div class="col-12 md:col-3">
        <Card class="patient-info-card p-shadow-2 mb-3">
          <template #content>
            <div class="text-center pb-3 border-bottom-1 surface-border">
              <Avatar icon="pi pi-user" size="xlarge" shape="circle" class="bg-primary text-white mb-2" />
              <h2 class="text-xl font-bold text-900 m-0">{{ patient.name || 'Carregando...' }}</h2>
              <span v-if="patient.socialName" class="text-sm text-primary font-semibold block">({{ patient.socialName }})</span>
              <span class="text-xs text-500">ID: {{ patient.id?.substring(0, 8) }}</span>
            </div>

            <div class="py-3 text-sm">
              <p class="my-2"><strong>CPF:</strong> {{ patient.cpf || 'Não informado' }}</p>
              <p class="my-2"><strong>Nascimento:</strong> {{ formatDate(patient.birthDate) }}</p>
              <p class="my-2"><strong>Telefone:</strong> {{ patient.phone || 'Não informado' }}</p>
              <p class="my-2"><strong>E-mail:</strong> {{ patient.email || 'Não informado' }}</p>
              <p class="my-2"><strong>Gênero:</strong> {{ patient.gender || 'Não informado' }}</p>
              <p class="my-2"><strong>Profissão:</strong> {{ patient.profession || 'Não informado' }}</p>
              <p class="my-2"><strong>Emergência:</strong> {{ patient.emergencyContact || 'Não informado' }}</p>
              <p v-if="patient.marketingOrigin" class="my-2"><strong>Origem:</strong> {{ patient.marketingOrigin }}</p>
            </div>

            <div class="border-top-1 surface-border pt-2 text-center">
              <Button label="Editar Ficha Cadastral" icon="pi pi-user-edit" text size="small" @click="showEditModal = true" />
            </div>
          </template>
        </Card>
      </div>

      <!-- Coluna da Direita: As 5 Abas do Prontuário Avançado -->
      <div class="col-12 md:col-9">
        <TabView v-model:activeIndex="activeTabIndex">
          <!-- ABA 1: RESUMO / DASHBOARD CLÍNICO -->
          <TabPanel header="1. Resumo Clínico">
            <div class="grid">
              <!-- Banner Alergias (Destaque Vermelho) -->
              <div class="col-12">
                <div class="allergy-banner p-3 border-round border-left-4 border-red-500 bg-red-50 text-red-900 flex justify-content-between align-items-center mb-3">
                  <div class="flex align-items-center gap-2">
                    <i class="pi pi-exclamation-triangle text-red-600 text-2xl"></i>
                    <div>
                      <strong class="text-base uppercase tracking-wide block">Alergias Conhecidas</strong>
                      <span class="text-sm">{{ patient.allergies || 'Nenhuma alergia relatada pelo paciente até o momento.' }}</span>
                    </div>
                  </div>
                  <Button v-if="authStore.isDoctor" label="Editar Resumo" icon="pi pi-pencil" text severity="danger" size="small" @click="openClinicalSummaryModal" />
                </div>
              </div>

              <!-- Cartões de Resumo Clínico -->
              <div class="col-12 md:col-4">
                <Card class="surface-card border-1 surface-border border-round h-full">
                  <template #title>
                    <div class="text-sm font-bold text-700 flex align-items-center gap-2">
                      <i class="pi pi-heart text-red-500"></i>
                      <span>Tipo Sanguíneo</span>
                    </div>
                  </template>
                  <template #content>
                    <div class="text-center py-2">
                      <span class="text-3xl font-extrabold text-red-600 bg-red-50 px-3 py-1 border-round border-1 border-red-200">
                        {{ patient.bloodType || 'N/I' }}
                      </span>
                    </div>
                  </template>
                </Card>
              </div>

              <div class="col-12 md:col-4">
                <Card class="surface-card border-1 surface-border border-round h-full">
                  <template #title>
                    <div class="text-sm font-bold text-700 flex align-items-center gap-2">
                      <i class="pi pi-info-circle text-blue-500"></i>
                      <span>Doenças Crônicas</span>
                    </div>
                  </template>
                  <template #content>
                    <p class="text-sm text-700 m-0">{{ patient.chronicDiseases || 'Nenhuma doença crônica cadastrada.' }}</p>
                  </template>
                </Card>
              </div>

              <div class="col-12 md:col-4">
                <Card class="surface-card border-1 surface-border border-round h-full">
                  <template #title>
                    <div class="text-sm font-bold text-700 flex align-items-center gap-2">
                      <i class="pi pi-shield text-purple-500"></i>
                      <span>Uso Contínuo</span>
                    </div>
                  </template>
                  <template #content>
                    <p class="text-sm text-700 m-0">{{ patient.continuousMedications || 'Nenhum medicamento contínuo relatado.' }}</p>
                  </template>
                </Card>
              </div>

              <div class="col-12 mt-2">
                <div class="p-3 border-round bg-yellow-50 border-1 border-yellow-200 text-yellow-900 flex align-items-center gap-2">
                  <i class="pi pi-bell text-yellow-600 text-xl"></i>
                  <div>
                    <strong>Alertas e Recomendações Clínicas:</strong>
                    <span class="text-sm ml-1">{{ patient.medicalAlerts || 'Sem alertas especiais registrados para este paciente.' }}</span>
                  </div>
                </div>
              </div>
            </div>
          </TabPanel>

          <!-- ABA 2: EVOLUÇÕES / ATENDIMENTOS (SOAP E ESPECIALIDADES) -->
          <TabPanel header="2. Evoluções (SOAP)">
            <div v-if="authStore.isDoctor">
              <div class="flex justify-content-between align-items-center mb-4">
                <h3 class="text-lg font-bold text-900 m-0"><i class="pi pi-file-edit mr-2 text-primary"></i> Histórico de Evoluções e Atendimentos</h3>
                <Button label="+ Nova Evolução / SOAP" icon="pi pi-plus" class="p-button-success p-button-sm" @click="showRecordModal = true" />
              </div>

              <div v-if="medicalRecords.length === 0" class="text-center p-5 surface-100 border-round text-600">
                <i class="pi pi-folder-open text-4xl mb-2 block"></i>
                Nenhuma anotação de prontuário registrada para este paciente.
              </div>

              <div v-else class="timeline">
                <div v-for="record in medicalRecords" :key="record.id" class="record-card surface-card p-4 border-round p-shadow-2 mb-3 border-left-4 border-primary">
                  <div class="flex justify-content-between align-items-center mb-2">
                    <span class="font-bold text-primary"><i class="pi pi-calendar mr-1"></i> {{ formatDate(record.createdAt) }}</span>
                    <small class="text-500 font-semibold">Registro Imutável e Assinado Digitalmente</small>
                  </div>
                  <h4 class="text-md font-semibold text-900 mt-2 mb-1">Diagnóstico / Avaliação (CID):</h4>
                  <p class="text-700 bg-blue-50 p-2 border-round text-sm m-0 mb-2 font-medium">{{ record.diagnosis }}</p>
                  <h4 class="text-md font-semibold text-900 mt-2 mb-1">Evolução Clínica / SOAP:</h4>
                  <p class="text-700 text-sm m-0 white-space-pre-line">{{ record.description }}</p>
                </div>
              </div>
            </div>
            <div v-else class="p-4 bg-orange-50 border-1 border-orange-200 border-round text-orange-900 text-center">
              <i class="pi pi-lock text-3xl mb-2 block text-orange-600"></i>
              <h4 class="m-0 font-bold">Acesso Restrito ao Prontuário</h4>
              <p class="text-sm m-0 mt-1">Conforme os requisitos de confidencialidade (LGPD/CFM), os prontuários médicos são acessíveis apenas pelos Profissionais de Saúde autorizados.</p>
            </div>
          </TabPanel>

          <!-- ABA 3: PRONTUÁRIO VISUAL / MARCADORES INTERATIVOS -->
          <TabPanel header="3. Prontuário Visual">
            <div v-if="authStore.isDoctor">
              <AnatomicalMarkerCanvas :patientId="patientId" />
            </div>
            <div v-else class="p-4 bg-orange-50 border-1 border-orange-200 border-round text-orange-900 text-center">
              A demarcação de pontos clínicos e estéticos é restrita aos profissionais de saúde.
            </div>
          </TabPanel>

          <!-- ABA 4: EXAMES E ANEXOS (FOTOS ANTES E DEPOIS) -->
          <TabPanel header="4. Exames & Fotos">
            <div class="flex justify-content-between align-items-center mb-3">
              <div>
                <h3 class="text-lg font-bold text-900 m-0">Anexos, Laudos e Fotos Antes/Depois</h3>
                <p class="text-500 m-0 text-sm">Armazene exames em PDF e registre a evolução estética e dermatológica do paciente.</p>
              </div>
              <Button label="+ Anexar Arquivo / Foto" icon="pi pi-upload" class="p-button-primary p-button-sm" @click="showAttachmentModal = true" />
            </div>

            <div v-if="attachments.length === 0" class="text-center p-5 surface-100 border-round text-500">
              <i class="pi pi-images text-4xl mb-2 block"></i>
              Nenhum exame ou foto anexado para este paciente.
            </div>

            <div v-else class="grid">
              <div v-for="att in attachments" :key="att.id" class="col-12 md:col-6 lg:col-4">
                <Card class="surface-card border-1 surface-border border-round h-full p-shadow-1">
                  <template #content>
                    <div v-if="att.contentType?.startsWith('image/')" class="text-center mb-2">
                      <img :src="att.fileUrl" :alt="att.title" class="border-round max-w-full max-h-12rem object-contain" />
                    </div>
                    <div v-else class="text-center py-4 surface-50 border-round mb-2">
                      <i class="pi pi-file-pdf text-red-500 text-5xl"></i>
                    </div>
                    <strong class="text-900 block text-base">{{ att.title }}</strong>
                    <div class="flex justify-content-between align-items-center mt-2">
                      <Tag :value="att.category" severity="info" />
                      <small class="text-500">{{ formatDate(att.createdAt) }}</small>
                    </div>
                    <p v-if="att.notes" class="text-xs text-600 mt-2 mb-0 italic">{{ att.notes }}</p>
                    <div class="flex justify-content-end gap-2 mt-3">
                      <a :href="att.fileUrl" target="_blank" class="p-button p-button-sm p-button-outlined p-button-info">
                        <i class="pi pi-external-link mr-1"></i> Abrir
                      </a>
                      <Button icon="pi pi-trash" text severity="danger" size="small" @click="deleteAttachment(att.id)" />
                    </div>
                  </template>
                </Card>
              </div>
            </div>
          </TabPanel>

          <!-- ABA 5: DOCUMENTOS EMITIDOS & RECEITUÁRIO -->
          <TabPanel header="5. Documentos & Receitas">
            <div v-if="authStore.isDoctor">
              <div class="flex justify-content-between align-items-center mb-4">
                <div>
                  <h3 class="text-lg font-bold text-900 m-0">Emissão de Documentos Clínicos</h3>
                  <p class="text-500 m-0 text-sm">Receitas simples, controle especial, atestados médicos e termos com logomarca e assinatura.</p>
                </div>
                <div class="flex gap-2">
                  <Button label="+ Novo Atestado / Termo" icon="pi pi-file" class="p-button-secondary p-button-sm" @click="showDocModal = true" />
                  <Button label="+ Emitir Receita Médica" icon="pi pi-file-pdf" class="p-button-primary p-button-sm" @click="showPrescriptionModal = true" />
                </div>
              </div>

              <!-- Lista de Documentos Emitidos -->
              <h4 class="text-md font-bold text-800 mb-2">Histórico de Documentos Gerados</h4>
              <div v-if="medicalDocuments.length === 0" class="text-center p-4 surface-50 border-round text-500 text-sm mb-4">
                Nenhum atestado ou termo emitido recentemente para este paciente.
              </div>
              <div v-else class="mb-4">
                <DataTable :value="medicalDocuments" responsiveLayout="scroll" class="p-datatable-sm">
                  <Column field="title" header="Título / Documento" sortable></Column>
                  <Column field="documentType" header="Tipo" sortable>
                    <template #body="{ data }">
                      <Tag :value="data.documentType" severity="warning" />
                    </template>
                  </Column>
                  <Column field="createdAt" header="Data" sortable>
                    <template #body="{ data }">{{ formatDate(data.createdAt) }}</template>
                  </Column>
                  <Column field="professionalName" header="Profissional Emissor">
                    <template #body="{ data }">
                      {{ data.professionalName }} ({{ data.professionalCouncil || 'CRM' }}: {{ data.professionalCrm }})
                    </template>
                  </Column>
                  <Column header="Ação" style="width: 100px;">
                    <template #body="{ data }">
                      <Button icon="pi pi-eye" text rounded severity="info" size="small" @click="viewDocumentDetails(data)" title="Visualizar" />
                    </template>
                  </Column>
                </DataTable>
              </div>
            </div>
            <div v-else class="p-4 bg-orange-50 border-1 border-orange-200 border-round text-orange-900 text-center">
              A emissão de receitas e documentos clínicos é restrita ao profissional de saúde autorizada.
            </div>
          </TabPanel>
        </TabView>
      </div>
    </div>

    <!-- MODAL NOVA EVOLUÇÃO SOAP & ESPECIALIDADES -->
    <Dialog header="Nova Evolução Clínica — Estrutura SOAP" v-model:visible="showRecordModal" :modal="true" :style="{ width: 'min(94vw, 750px)' }">
      <div class="p-fluid">
        <!-- Preset de Especialidade -->
        <div class="flex gap-2 mb-3 pb-2 border-bottom-1 surface-border">
          <Button label="Médico (SOAP)" size="small" :outlined="recordSpecialty !== 'Medico'" @click="setRecordSpecialty('Medico')" />
          <Button label="Fisioterapia" size="small" :outlined="recordSpecialty !== 'Fisioterapia'" @click="setRecordSpecialty('Fisioterapia')" />
          <Button label="Estética" size="small" :outlined="recordSpecialty !== 'Estetica'" @click="setRecordSpecialty('Estetica')" />
          <Button label="Nutrição (IMC)" size="small" :outlined="recordSpecialty !== 'Nutricao'" @click="setRecordSpecialty('Nutricao')" />
        </div>

        <div class="p-field mb-3">
          <label for="recordAppointment" class="font-semibold text-700 mb-1 block">Consulta Realizada *</label>
          <select id="recordAppointment" v-model="newRecord.appointmentId" class="w-full p-2 border-round surface-border" required>
            <option value="">Selecione a consulta vinculada</option>
            <option v-for="appointment in completedAppointments" :key="appointment.id" :value="appointment.id">
              {{ formatDate(appointment.startTime) }} — Consulta #{{ appointment.id.substring(0, 8) }}
            </option>
          </select>
        </div>

        <!-- Calculadora Nutricional se Nutrição -->
        <div v-if="recordSpecialty === 'Nutricao'" class="p-3 mb-3 bg-green-50 border-round border-1 border-green-200">
          <strong class="text-green-900 block mb-2"><i class="pi pi-calculator mr-1"></i> Calculadora Antropométrica (IMC Automático)</strong>
          <div class="grid">
            <div class="col-4">
              <label class="text-xs font-semibold block mb-1">Peso (kg)</label>
              <InputNumber v-model="nutriWeight" :minFractionDigits="1" placeholder="Ex: 70.5" @input="calcImc" />
            </div>
            <div class="col-4">
              <label class="text-xs font-semibold block mb-1">Altura (cm)</label>
              <InputNumber v-model="nutriHeight" placeholder="Ex: 172" @input="calcImc" />
            </div>
            <div class="col-4 flex flex-column justify-content-center text-center">
              <span class="text-xs text-600 block">IMC Calculado:</span>
              <strong class="text-xl text-green-700">{{ calculatedImc || '--' }}</strong>
              <small class="text-500 font-semibold">{{ imcClassification }}</small>
            </div>
          </div>
        </div>

        <!-- Escala Fitzpatrick se Estética -->
        <div v-if="recordSpecialty === 'Estetica'" class="p-3 mb-3 bg-purple-50 border-round border-1 border-purple-200">
          <strong class="text-purple-900 block mb-2"><i class="pi pi-palette mr-1"></i> Avaliação Facial & Escala de Fitzpatrick</strong>
          <div class="grid">
            <div class="col-6">
              <label class="text-xs font-semibold block mb-1">Fototipo (Fitzpatrick)</label>
              <Dropdown v-model="fitzpatrick" :options="['Tipo I (Muito Clara)', 'Tipo II (Clara)', 'Tipo III (Morena Clara)', 'Tipo IV (Morena Moderada)', 'Tipo V (Escura)', 'Tipo VI (Negra)']" placeholder="Selecione o fototipo" />
            </div>
            <div class="col-6">
              <label class="text-xs font-semibold block mb-1">Região / Procedimento</label>
              <InputText v-model="aestheticArea" placeholder="Ex: Terço superior / Toxina" />
            </div>
          </div>
        </div>

        <!-- Escala de Dor se Fisioterapia -->
        <div v-if="recordSpecialty === 'Fisioterapia'" class="p-3 mb-3 bg-blue-50 border-round border-1 border-blue-200">
          <strong class="text-blue-900 block mb-2"><i class="pi pi-chart-line mr-1"></i> Escala Visual Analógica de Dor (EVA) & Postura</strong>
          <div class="grid">
            <div class="col-6">
              <label class="text-xs font-semibold block mb-1">Nível de Dor Atual (0 a 10)</label>
              <InputNumber v-model="painScale" :min="0" :max="10" placeholder="0 = Sem dor, 10 = Máxima dor" />
            </div>
            <div class="col-6">
              <label class="text-xs font-semibold block mb-1">Número da Sessão</label>
              <InputText v-model="physioSession" placeholder="Ex: Sessão 3 de 10" />
            </div>
          </div>
        </div>

        <div class="p-field mb-3">
          <label for="diagnosis" class="font-semibold text-700 mb-1 block">Avaliação / Diagnóstico / CID-10 *</label>
          <InputText id="diagnosis" v-model="newRecord.diagnosis" placeholder="Ex: CID10 M54.5 - Dor lombar baixa / CID10 J00 - Nasofaringite" required />
        </div>

        <div class="p-field mb-3">
          <label for="description" class="font-semibold text-700 mb-1 block">Evolução Clínica (SOAP: Subjetivo, Objetivo, Avaliação, Plano) *</label>
          <Textarea id="description" v-model="newRecord.description" rows="6" placeholder="S: Queixa do paciente...
O: Exame físico e sinais vitais...
A: Diagnóstico e raciocínio clínico...
P: Plano de tratamento, conduta e prescrições..." required />
        </div>
      </div>
      <template #footer>
        <Button label="Cancelar" icon="pi pi-times" class="p-button-text" @click="showRecordModal = false" />
        <Button label="Assinar & Salvar no Prontuário" icon="pi pi-check" class="p-button-success" :loading="savingRecord" @click="saveMedicalRecord" />
      </template>
    </Dialog>

    <!-- MODAL EDITAR RESUMO CLÍNICO -->
    <Dialog header="Atualizar Resumo Clínico do Paciente" v-model:visible="showSummaryModal" :modal="true" :style="{ width: '550px' }">
      <div class="p-fluid">
        <div class="mb-3">
          <label class="font-semibold block mb-1 text-red-600"><i class="pi pi-exclamation-triangle mr-1"></i>Alergias</label>
          <InputText v-model="summaryForm.allergies" placeholder="Penicilina, Dipirona, Látex..." />
        </div>
        <div class="grid">
          <div class="col-6 mb-3">
            <label class="font-semibold block mb-1">Tipo Sanguíneo</label>
            <Dropdown v-model="summaryForm.bloodType" :options="['A+', 'A-', 'B+', 'B-', 'AB+', 'AB-', 'O+', 'O-', 'Desconhecido']" />
          </div>
          <div class="col-6 mb-3">
            <label class="font-semibold block mb-1">Doenças Crônicas</label>
            <InputText v-model="summaryForm.chronicDiseases" placeholder="Hipertensão, Diabetes..." />
          </div>
        </div>
        <div class="mb-3">
          <label class="font-semibold block mb-1">Medicamentos Contínuos</label>
          <InputText v-model="summaryForm.continuousMedications" placeholder="Ex: Losartana 50mg" />
        </div>
        <div class="mb-3">
          <label class="font-semibold block mb-1 text-yellow-700">Alertas Clínicos Especiais</label>
          <InputText v-model="summaryForm.medicalAlerts" placeholder="Ex: Gestante, Marcapasso, Histórico de Quelóide" />
        </div>
      </div>
      <template #footer>
        <Button label="Cancelar" icon="pi pi-times" text @click="showSummaryModal = false" />
        <Button label="Salvar Resumo" icon="pi pi-check" :loading="savingSummary" class="p-button-success" @click="saveClinicalSummary" />
      </template>
    </Dialog>

    <!-- MODAL ANEXAR EXAME / FOTO -->
    <Dialog header="Anexar Exame / Foto Antes e Depois" v-model:visible="showAttachmentModal" :modal="true" :style="{ width: '500px' }">
      <div class="p-fluid">
        <div class="mb-3">
          <label class="font-semibold block mb-1">Título do Documento / Foto *</label>
          <InputText v-model="attachmentForm.title" placeholder="Ex: Foto Antes - Terço Superior, Hemograma Completo" required />
        </div>
        <div class="mb-3">
          <label class="font-semibold block mb-1">Categoria *</label>
          <Dropdown v-model="attachmentForm.category" :options="['Foto Antes/Depois', 'Exame Laboratorial', 'Laudo / Imagem', 'Outros']" />
        </div>
        <div class="mb-3">
          <label class="font-semibold block mb-1">Arquivo / Imagem *</label>
          <input type="file" @change="handleFileUpload" class="p-2 border-1 surface-border border-round w-full" accept="image/*,application/pdf" />
          <small class="text-500">Suporta fotos (JPG/PNG) ou documentos em PDF.</small>
        </div>
        <div class="mb-3">
          <label class="font-semibold block mb-1">Observações Clínicas (opcional)</label>
          <InputText v-model="attachmentForm.notes" placeholder="Ex: Foto tirada no pré-procedimento imediato" />
        </div>
      </div>
      <template #footer>
        <Button label="Cancelar" icon="pi pi-times" text @click="showAttachmentModal = false" />
        <Button label="Salvar Anexo" icon="pi pi-check" :loading="savingAttachment" class="p-button-success" @click="saveAttachment" />
      </template>
    </Dialog>

    <!-- MODAL EMITIR ATESTADO / TERMO -->
    <Dialog header="Emitir Documento Clínico (Atestado / Termo)" v-model:visible="showDocModal" :modal="true" :style="{ width: '600px' }">
      <div class="p-fluid">
        <div class="mb-3">
          <label class="font-semibold block mb-1">Tipo de Documento *</label>
          <Dropdown v-model="docForm.documentType" :options="['Atestado Médico', 'Solicitação de Exames', 'Termo de Consentimento (TCLE)', 'Declaração de Comparecimento']" @change="handleDocTypeChange" />
        </div>
        <div class="mb-3">
          <label class="font-semibold block mb-1">Título *</label>
          <InputText v-model="docForm.title" placeholder="Ex: Atestado de Afastamento" required />
        </div>
        <div v-if="docForm.documentType === 'Atestado Médico'" class="grid">
          <div class="col-6 mb-3">
            <label class="font-semibold block mb-1">Dias de Repouso / Afastamento</label>
            <InputNumber v-model="docForm.daysOff" :min="1" :max="90" suffix=" dias" />
          </div>
          <div class="col-6 mb-3">
            <label class="font-semibold block mb-1">CID-10 (opcional)</label>
            <InputText v-model="docForm.cid" placeholder="Ex: CID10 J00" />
          </div>
        </div>
        <div class="mb-3">
          <label class="font-semibold block mb-1">Texto do Documento / Declaração *</label>
          <Textarea v-model="docForm.content" rows="6" required />
        </div>
      </div>
      <template #footer>
        <Button label="Cancelar" icon="pi pi-times" text @click="showDocModal = false" />
        <Button label="Emitir Documento" icon="pi pi-check" :loading="savingDoc" class="p-button-success" @click="saveDocument" />
      </template>
    </Dialog>

    <!-- MODAL EMITIR RECEITA COM MODELOS -->
    <Dialog header="Emitir Receituário Médico & PDF" v-model:visible="showPrescriptionModal" :modal="true" :style="{ width: 'min(94vw, 750px)' }">
      <div class="p-fluid">
        <div class="flex justify-content-between align-items-center mb-3">
          <span class="text-sm font-semibold text-700">Modelos de Prescrição Frequentes:</span>
          <div class="flex gap-1">
            <Button label="Kit Pós-Op" size="small" outlined @click="applyPrescriptionKit('PosOp')" />
            <Button label="Hipertensão" size="small" outlined @click="applyPrescriptionKit('Hipertensao')" />
            <Button label="Analgesia" size="small" outlined @click="applyPrescriptionKit('Analgesia')" />
          </div>
        </div>

        <div class="mb-3">
          <label class="font-semibold block mb-1">Consulta Realizada *</label>
          <select v-model="newPrescription.appointmentId" class="w-full p-2 border-round surface-border" required>
            <option value="">Selecione a consulta realizada</option>
            <option v-for="appointment in prescriptionAppointments" :key="appointment.id" :value="appointment.id">
              {{ formatDate(appointment.startTime) }} — Consulta #{{ appointment.id.substring(0, 8) }}
            </option>
          </select>
        </div>

        <div class="mb-3">
          <label class="font-semibold block mb-1">Orientações Gerais da Receita</label>
          <Textarea v-model="newPrescription.generalNotes" rows="2" placeholder="Ex: Ingerir com água após as refeições. Retornar em 15 dias." />
        </div>

        <div v-for="(item, index) in newPrescription.items" :key="index" class="prescription-item p-3 border-round surface-50 mb-3 border-1 surface-border">
          <div class="flex justify-content-between align-items-center mb-2">
            <strong class="text-sm text-primary">Medicamento {{ index + 1 }}</strong>
            <Button v-if="newPrescription.items.length > 1" icon="pi pi-trash" text severity="danger" size="small" @click="removePrescriptionItem(index)" />
          </div>
          <div class="grid">
            <div class="col-12 md:col-6 mb-2">
              <label class="text-xs font-semibold block mb-1">Nome do Medicamento *</label>
              <InputText v-model="item.medicationName" placeholder="Ex: Amoxicilina + Clavulanato" required />
            </div>
            <div class="col-12 md:col-6 mb-2">
              <label class="text-xs font-semibold block mb-1">Concentração / Dosagem *</label>
              <InputText v-model="item.dosage" placeholder="Ex: 875mg / 125mg" required />
            </div>
            <div class="col-12 md:col-4 mb-2">
              <label class="text-xs font-semibold block mb-1">Posologia / Frequência *</label>
              <InputText v-model="item.frequency" placeholder="Ex: 1 comprimido de 12/12h" required />
            </div>
            <div class="col-12 md:col-4 mb-2">
              <label class="text-xs font-semibold block mb-1">Duração do Tratamento *</label>
              <InputText v-model="item.duration" placeholder="Ex: Por 7 dias" required />
            </div>
            <div class="col-12 md:col-4 mb-2">
              <label class="text-xs font-semibold block mb-1">Via de Administração *</label>
              <InputText v-model="item.route" placeholder="Ex: Via Oral" required />
            </div>
          </div>
        </div>

        <Button label="+ Adicionar Outro Medicamento" icon="pi pi-plus" text size="small" @click="addPrescriptionItem" />
      </div>
      <template #footer>
        <Button label="Cancelar" icon="pi pi-times" text @click="showPrescriptionModal = false" />
        <Button label="Emitir & Baixar PDF" icon="pi pi-file-pdf" :loading="savingPrescription" class="p-button-success" @click="createPrescription" />
      </template>
    </Dialog>

    <!-- Modal Editar Paciente -->
    <PatientFormModal
      :visible="showEditModal"
      :patientData="patient"
      @close="showEditModal = false"
      @save="handleSavePatient"
    />

    <!-- Modal Acesso Portal -->
    <Dialog header="Acesso ao Portal do Paciente" v-model:visible="showPortalCredentials" :modal="true" :style="{ width: 'min(94vw, 500px)' }">
      <p class="text-sm text-700">Entregue esta senha temporária ao paciente por um canal seguro. Ela será exibida apenas agora:</p>
      <div v-if="portalCredentials" class="p-3 bg-blue-50 border-round border-1 border-blue-200">
        <div><strong>Login:</strong> {{ portalCredentials.email }}</div>
        <div><strong>Senha Temporária:</strong> <code class="text-primary font-bold text-lg">{{ portalCredentials.temporaryPassword }}</code></div>
      </div>
      <template #footer>
        <Button label="Copiar Senha" icon="pi pi-copy" text @click="copyTemporaryPassword" />
        <Button label="Fechar" @click="showPortalCredentials = false" />
      </template>
    </Dialog>
  </div>
</template>

<script setup>
import { computed, ref, onMounted } from 'vue';
import { useRoute } from 'vue-router';
import { useToast } from 'primevue/usetoast';
import Button from 'primevue/button';
import Card from 'primevue/card';
import Avatar from 'primevue/avatar';
import TabView from 'primevue/tabview';
import TabPanel from 'primevue/tabpanel';
import Dialog from 'primevue/dialog';
import InputText from 'primevue/inputtext';
import InputNumber from 'primevue/inputnumber';
import Textarea from 'primevue/textarea';
import Dropdown from 'primevue/dropdown';
import Tag from 'primevue/tag';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import Toast from 'primevue/toast';

import PatientFormModal from '@/components/PatientFormModal.vue';
import AnatomicalMarkerCanvas from '@/components/AnatomicalMarkerCanvas.vue';
import { patientsService } from '@/api/patients.service';
import { medicalRecordsService } from '@/api/medicalRecords.service';
import { appointmentsService } from '@/api/appointments.service';
import { professionalsService } from '@/api/professionals.service';
import { prescriptionsService } from '@/api/prescriptions.service';
import { medicalDocumentsService } from '@/api/medicalDocuments.service';
import { useAuthStore } from '@/stores/auth.store';

const toast = useToast();
const route = useRoute();
const authStore = useAuthStore();
const patientId = route.params.id;

const activeTabIndex = ref(0);
const patient = ref({});
const medicalRecords = ref([]);
const completedAppointments = ref([]);
const professionalId = ref('');
const attachments = ref([]);
const medicalDocuments = ref([]);

const showEditModal = ref(false);
const showRecordModal = ref(false);
const savingRecord = ref(false);
const showSummaryModal = ref(false);
const savingSummary = ref(false);
const showAttachmentModal = ref(false);
const savingAttachment = ref(false);
const showDocModal = ref(false);
const savingDoc = ref(false);
const showPrescriptionModal = ref(false);
const savingPrescription = ref(false);
const showPortalCredentials = ref(false);
const portalCredentials = ref(null);

const canCreatePortalAccount = ['Admin', 'Secretaria'].includes(authStore.user?.role);
const prescriptionAppointments = computed(() =>
  completedAppointments.value.filter(a => a.professionalId === professionalId.value)
);

// Prescrição
function newPrescriptionItem() {
  return { medicationName: '', dosage: '', frequency: '', duration: '', route: 'Via Oral', instructions: '' };
}
const newPrescription = ref({
  appointmentId: '',
  generalNotes: '',
  items: [newPrescriptionItem()]
});

// SOAP & Especialidades
const recordSpecialty = ref('Medico');
const newRecord = ref({ appointmentId: '', diagnosis: '', description: '' });
const nutriWeight = ref(null);
const nutriHeight = ref(null);
const calculatedImc = ref('');
const imcClassification = ref('');
const fitzpatrick = ref('');
const aestheticArea = ref('');
const painScale = ref(null);
const physioSession = ref('');

// Resumo Clínico
const summaryForm = ref({ bloodType: '', allergies: '', chronicDiseases: '', continuousMedications: '', medicalAlerts: '' });

// Anexos
const attachmentForm = ref({ title: '', category: 'Foto Antes/Depois', contentType: 'image/jpeg', fileUrl: '', notes: '' });

// Documentos
const docForm = ref({ documentType: 'Atestado Médico', title: 'Atestado Médico', content: '', cid: '', daysOff: 1 });

onMounted(async () => {
  await loadPatient();
  if (authStore.isDoctor) {
    await Promise.all([
      loadMedicalRecords(),
      loadCompletedAppointments(),
      loadCurrentProfessional(),
      loadAttachments(),
      loadMedicalDocuments()
    ]);
  } else {
    await loadAttachments();
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
      a => a.patientId === patientId && Number(a.status) === 3
    );
  } catch (err) {
    console.error('Erro ao buscar consultas realizadas', err);
  }
}

async function loadCurrentProfessional() {
  try {
    const professionals = await professionalsService.getAll();
    professionalId.value = professionals.find(
      p => p.email?.toLowerCase() === authStore.user?.email?.toLowerCase()
    )?.id || '';
  } catch (err) {
    console.error('Erro ao validar profissional', err);
  }
}

async function loadMedicalRecords() {
  try {
    medicalRecords.value = await medicalRecordsService.getRecordsByPatient(patientId);
  } catch (err) {
    console.error('Erro ao buscar prontuários', err);
  }
}

async function loadAttachments() {
  try {
    attachments.value = await patientsService.getAttachments(patientId);
  } catch (err) {
    console.error('Erro ao buscar anexos', err);
  }
}

async function loadMedicalDocuments() {
  try {
    medicalDocuments.value = await medicalDocumentsService.getByPatient(patientId);
  } catch (err) {
    console.error('Erro ao buscar documentos', err);
  }
}

function setRecordSpecialty(spec) {
  recordSpecialty.value = spec;
}

function calcImc() {
  if (nutriWeight.value && nutriHeight.value) {
    const heightM = nutriHeight.value / 100;
    const imc = nutriWeight.value / (heightM * heightM);
    calculatedImc.value = imc.toFixed(1);
    if (imc < 18.5) imcClassification.value = 'Abaixo do peso';
    else if (imc < 25) imcClassification.value = 'Peso normal / Eutrofia';
    else if (imc < 30) imcClassification.value = 'Sobrepeso';
    else imcClassification.value = 'Obesidade';
  }
}

function openClinicalSummaryModal() {
  summaryForm.value = {
    bloodType: patient.value.bloodType || '',
    allergies: patient.value.allergies || '',
    chronicDiseases: patient.value.chronicDiseases || '',
    continuousMedications: patient.value.continuousMedications || '',
    medicalAlerts: patient.value.medicalAlerts || ''
  };
  showSummaryModal.value = true;
}

async function saveClinicalSummary() {
  savingSummary.value = true;
  try {
    await patientsService.updateClinicalSummary(patientId, summaryForm.value);
    await loadPatient();
    toast.add({ severity: 'success', summary: 'Sucesso', detail: 'Resumo clínico atualizado.', life: 3000 });
    showSummaryModal.value = false;
  } catch (err) {
    toast.add({ severity: 'error', summary: 'Erro', detail: 'Erro ao salvar resumo clínico.', life: 3000 });
  } finally {
    savingSummary.value = false;
  }
}

async function saveMedicalRecord() {
  if (!newRecord.value.appointmentId || !newRecord.value.diagnosis || !newRecord.value.description) {
    toast.add({ severity: 'warn', summary: 'Atenção', detail: 'Preencha consulta, diagnóstico e descrição.', life: 3000 });
    return;
  }

  savingRecord.value = true;
  try {
    let finalDesc = newRecord.value.description;
    if (recordSpecialty.value === 'Nutricao' && calculatedImc.value) {
      finalDesc += `\n\n[Antropometria: Peso ${nutriWeight.value}kg, Altura ${nutriHeight.value}cm, IMC ${calculatedImc.value} (${imcClassification.value})]`;
    } else if (recordSpecialty.value === 'Estetica' && fitzpatrick.value) {
      finalDesc += `\n\n[Estética: Fototipo ${fitzpatrick.value}, Área: ${aestheticArea.value || 'Geral'}]`;
    } else if (recordSpecialty.value === 'Fisioterapia' && painScale.value !== null) {
      finalDesc += `\n\n[Fisioterapia: Escala de Dor EVA ${painScale.value}/10, Sessão: ${physioSession.value || 'N/A'}]`;
    }

    await medicalRecordsService.createRecord({
      patientId: patientId,
      appointmentId: newRecord.value.appointmentId,
      diagnosis: newRecord.value.diagnosis,
      description: finalDesc
    });
    newRecord.value = { appointmentId: '', diagnosis: '', description: '' };
    showRecordModal.value = false;
    await loadMedicalRecords();
    toast.add({ severity: 'success', summary: 'Sucesso', detail: 'Evolução registrada no prontuário imutável.', life: 3000 });
  } catch (err) {
    toast.add({ severity: 'error', summary: 'Erro', detail: err.response?.data?.detail || err.message, life: 4000 });
  } finally {
    savingRecord.value = false;
  }
}

function handleFileUpload(event) {
  const file = event.target.files[0];
  if (!file) return;

  attachmentForm.value.contentType = file.type || 'application/octet-stream';
  const reader = new FileReader();
  reader.onload = (e) => {
    attachmentForm.value.fileUrl = e.target.result;
  };
  reader.readAsDataURL(file);
}

async function saveAttachment() {
  if (!attachmentForm.value.title || !attachmentForm.value.fileUrl) {
    toast.add({ severity: 'warn', summary: 'Atenção', detail: 'Selecione um arquivo e informe o título.', life: 3000 });
    return;
  }

  savingAttachment.value = true;
  try {
    await patientsService.createAttachment(patientId, attachmentForm.value);
    showAttachmentModal.value = false;
    attachmentForm.value = { title: '', category: 'Foto Antes/Depois', contentType: 'image/jpeg', fileUrl: '', notes: '' };
    await loadAttachments();
    toast.add({ severity: 'success', summary: 'Sucesso', detail: 'Anexo salvo com sucesso.', life: 3000 });
  } catch (err) {
    toast.add({ severity: 'error', summary: 'Erro', detail: 'Erro ao anexar arquivo.', life: 3000 });
  } finally {
    savingAttachment.value = false;
  }
}

async function deleteAttachment(id) {
  try {
    await patientsService.deleteAttachment(patientId, id);
    await loadAttachments();
    toast.add({ severity: 'info', summary: 'Removido', detail: 'Anexo removido.', life: 2500 });
  } catch (err) {
    toast.add({ severity: 'error', summary: 'Erro', detail: 'Não foi possível remover o anexo.', life: 3000 });
  }
}

function handleDocTypeChange() {
  if (docForm.value.documentType === 'Atestado Médico') {
    docForm.value.title = 'Atestado Médico de Afastamento';
    docForm.value.content = `Atesto para os devidos fins que o(a) paciente ${patient.value.name}, inscrito(a) no CPF ${patient.value.cpf}, esteve sob meus cuidados profissionais nesta data e necessita de ${docForm.value.daysOff || 1} dia(s) de afastamento de suas atividades laborais.`;
  } else if (docForm.value.documentType === 'Solicitação de Exames') {
    docForm.value.title = 'Solicitação de Exames Laboratoriais';
    docForm.value.content = `Solicito para fins de investigação diagnóstica os seguintes exames:\n1. Hemograma completo com contagem de plaquetas\n2. Glicemia de jejum\n3. Perfil lipídico (Colesterol total e frações, Triglicerídeos)\n4. TSH e T4 Livre\n5. Ureia e Creatinina`;
  } else if (docForm.value.documentType === 'Termo de Consentimento (TCLE)') {
    docForm.value.title = 'Termo de Consentimento Livre e Esclarecido';
    docForm.value.content = `Eu, ${patient.value.name}, declaro que fui devidamente esclarecido(a) sobre a indicação, os objetivos, os riscos e os cuidados pós-procedimento recomendados pelo profissional de saúde, autorizando a realização do atendimento proposto.`;
  }
}

async function saveDocument() {
  if (!docForm.value.title || !docForm.value.content) {
    toast.add({ severity: 'warn', summary: 'Atenção', detail: 'Preencha o título e o conteúdo do documento.', life: 3000 });
    return;
  }

  savingDoc.value = true;
  try {
    await medicalDocumentsService.create({
      patientId,
      documentType: docForm.value.documentType,
      title: docForm.value.title,
      content: docForm.value.content,
      cid: docForm.value.cid || null,
      daysOff: docForm.value.daysOff || null
    });
    showDocModal.value = false;
    await loadMedicalDocuments();
    toast.add({ severity: 'success', summary: 'Sucesso', detail: 'Documento clínico emitido com sucesso.', life: 3000 });
  } catch (err) {
    toast.add({ severity: 'error', summary: 'Erro', detail: 'Erro ao emitir documento.', life: 3000 });
  } finally {
    savingDoc.value = false;
  }
}

function viewDocumentDetails(doc) {
  alert(`DOCUMENTO: ${doc.title}\n\n${doc.content}\n\nEmitido por: ${doc.professionalName || 'Profissional'}`);
}

function applyPrescriptionKit(kit) {
  if (kit === 'PosOp') {
    newPrescription.value.generalNotes = 'Kit Pós-Procedimento: Seguir horários rigorosamente por 5 dias.';
    newPrescription.value.items = [
      { medicationName: 'Dipirona Monoidratada', dosage: '500mg', frequency: '1 comprimido de 6/6h', duration: '3 dias (se dor)', route: 'Via Oral', instructions: 'Tomar com água' },
      { medicationName: 'Cetorolaco de Trometamina', dosage: '10mg', frequency: '1 comprimido sublingual de 8/8h', duration: '3 dias', route: 'Sublingual', instructions: 'Deixar dissolver sob a língua' }
    ];
  } else if (kit === 'Hipertensao') {
    newPrescription.value.generalNotes = 'Controle de Hipertensão Arterial Sistêmica';
    newPrescription.value.items = [
      { medicationName: 'Losartana Potássica', dosage: '50mg', frequency: '1 comprimido pela manhã', duration: 'Uso contínuo', route: 'Via Oral', instructions: 'Tomar em jejum' }
    ];
  } else if (kit === 'Analgesia') {
    newPrescription.value.generalNotes = 'Analgesia e Anti-inflamatório';
    newPrescription.value.items = [
      { medicationName: 'Paracetamol', dosage: '750mg', frequency: '1 comprimido de 8/8h', duration: '4 dias', route: 'Via Oral', instructions: 'Em caso de dor leve/moderada' },
      { medicationName: 'Ibuprofeno', dosage: '600mg', frequency: '1 comprimido de 12/12h', duration: '3 dias', route: 'Via Oral', instructions: 'Tomar após as refeições' }
    ];
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
  if (!professionalId.value || !newPrescription.value.appointmentId) {
    toast.add({ severity: 'warn', summary: 'Atenção', detail: 'Selecione a consulta vinculada para emitir a receita.', life: 3000 });
    return;
  }

  savingPrescription.value = true;
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
        instructions: item.instructions?.trim() || null
      }))
    });
    const pdf = await prescriptionsService.downloadPdf(result.id);
    const url = URL.createObjectURL(pdf);
    const link = document.createElement('a');
    link.href = url;
    link.download = `receita-${result.id}.pdf`;
    link.click();
    window.setTimeout(() => URL.revokeObjectURL(url), 1000);

    showPrescriptionModal.value = false;
    toast.add({ severity: 'success', summary: 'Sucesso', detail: 'Receita emitida e baixada com sucesso.', life: 3000 });
  } catch (err) {
    toast.add({ severity: 'error', summary: 'Erro', detail: err.response?.data?.detail || 'Erro ao emitir receita.', life: 4000 });
  } finally {
    savingPrescription.value = false;
  }
}

async function handleSavePatient(formData) {
  try {
    await patientsService.updatePatient(patientId, formData);
    await loadPatient();
    showEditModal.value = false;
    toast.add({ severity: 'success', summary: 'Sucesso', detail: 'Ficha do paciente atualizada com sucesso.', life: 3000 });
  } catch (err) {
    toast.add({ severity: 'error', summary: 'Erro', detail: 'Erro ao salvar paciente.', life: 3000 });
  }
}

async function createPortalAccount() {
  try {
    portalCredentials.value = await patientsService.createPortalAccount(patientId);
    showPortalCredentials.value = true;
    patient.value.hasPortalAccount = true;
  } catch (err) {
    toast.add({ severity: 'error', summary: 'Erro', detail: err.response?.data?.detail || 'Erro ao criar conta do portal.', life: 4000 });
  }
}

async function copyTemporaryPassword() {
  if (portalCredentials.value?.temporaryPassword) {
    await navigator.clipboard.writeText(portalCredentials.value.temporaryPassword);
    toast.add({ severity: 'info', summary: 'Copiado', detail: 'Senha provisória copiada.', life: 2000 });
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
.header-actions {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}
.allergy-banner {
  box-shadow: 0 1px 3px rgba(239, 68, 68, 0.15);
}
.record-card {
  transition: transform 0.2s ease;
}
.record-card:hover {
  transform: translateY(-2px);
}
</style>
