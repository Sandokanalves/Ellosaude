<template>
  <Dialog :header="isEditMode ? 'Editar Paciente' : 'Novo Paciente'" v-model:visible="isModalVisible" :modal="true" :style="{ width: 'min(94vw, 700px)' }" @hide="closeModal">
    <form @submit.prevent="onSubmit">
      <div class="p-fluid">
        <h4 class="text-sm font-bold text-700 m-0 mb-2 border-bottom-1 surface-border pb-1">1. Dados Administrativos</h4>
        <div class="grid">
          <div class="col-12 md:col-6 mb-2">
            <label for="name" class="font-semibold text-700 mb-1 block">Nome Completo *</label>
            <InputText id="name" v-model="name" placeholder="Ex: Maria Santos" :class="{ 'p-invalid': errors.name }" />
            <small class="p-error">{{ errors.name }}</small>
          </div>

          <div class="col-12 md:col-6 mb-2">
            <label for="socialName" class="font-semibold text-700 mb-1 block">Nome Social (opcional)</label>
            <InputText id="socialName" v-model="socialName" placeholder="Nome como prefere ser chamado" />
          </div>

          <div class="col-12 md:col-6 mb-2">
            <label for="cpf" class="font-semibold text-700 mb-1 block">CPF *</label>
            <InputText id="cpf" v-model="cpf" placeholder="000.000.000-00" :class="{ 'p-invalid': errors.cpf }" />
            <small class="p-error">{{ errors.cpf }}</small>
          </div>

          <div class="col-12 md:col-6 mb-2">
            <label for="birthDate" class="font-semibold text-700 mb-1 block">Data de Nascimento *</label>
            <Calendar id="birthDate" v-model="birthDate" dateFormat="dd/mm/yy" showIcon placeholder="dd/mm/aaaa" :class="{ 'p-invalid': errors.birthDate }" />
            <small class="p-error">{{ errors.birthDate }}</small>
          </div>

          <div class="col-12 md:col-6 mb-2">
            <label for="email" class="font-semibold text-700 mb-1 block">E-mail *</label>
            <InputText id="email" v-model="email" placeholder="maria@email.com" :class="{ 'p-invalid': errors.email }" />
            <small class="p-error">{{ errors.email }}</small>
          </div>

          <div class="col-12 md:col-6 mb-2">
            <label for="phone" class="font-semibold text-700 mb-1 block">Telefone / WhatsApp</label>
            <InputText id="phone" v-model="phone" placeholder="(81) 99999-9999" maxlength="30" />
          </div>

          <div class="col-12 md:col-6 mb-2">
            <label for="gender" class="font-semibold text-700 mb-1 block">Gênero</label>
            <Dropdown id="gender" v-model="gender" :options="['Feminino', 'Masculino', 'Não-binário', 'Outro', 'Prefiro não informar']" placeholder="Selecione" />
          </div>

          <div class="col-12 md:col-6 mb-2">
            <label for="address" class="font-semibold text-700 mb-1 block">Endereço Residencial</label>
            <InputText id="address" v-model="address" placeholder="Rua, número, bairro, cidade" />
          </div>
        </div>

        <h4 class="text-sm font-bold text-700 m-0 mt-2 mb-2 border-bottom-1 surface-border pb-1">2. Dados Complementares & Marketing</h4>
        <div class="grid">
          <div class="col-12 md:col-6 mb-2">
            <label for="profession" class="font-semibold text-700 mb-1 block">Profissão</label>
            <InputText id="profession" v-model="profession" placeholder="Ex: Advogada, Engenheiro" />
          </div>

          <div class="col-12 md:col-6 mb-2">
            <label for="emergencyContact" class="font-semibold text-700 mb-1 block">Contato de Emergência</label>
            <InputText id="emergencyContact" v-model="emergencyContact" placeholder="Nome e Telefone (ex: Mãe - 81 98888-0000)" />
          </div>

          <div class="col-12 md:col-6 mb-2">
            <label for="legalGuardian" class="font-semibold text-700 mb-1 block">Responsável Legal (se menor)</label>
            <InputText id="legalGuardian" v-model="legalGuardian" placeholder="Nome do pai/mãe ou responsável legal" />
          </div>

          <div class="col-12 md:col-6 mb-2">
            <label for="marketingOrigin" class="font-semibold text-700 mb-1 block">Origem do Paciente (Marketing)</label>
            <Dropdown id="marketingOrigin" v-model="marketingOrigin" :options="['Instagram / Redes Sociais', 'Google / Busca', 'Indicação de Amigo/Familiar', 'Passagem pela Clínica', 'Convênio', 'Outros']" placeholder="Como nos conheceu?" editable />
          </div>
        </div>

        <h4 class="text-sm font-bold text-700 m-0 mt-2 mb-2 border-bottom-1 surface-border pb-1">3. Resumo Clínico Inicial</h4>
        <div class="grid">
          <div class="col-12 md:col-4 mb-2">
            <label for="bloodType" class="font-semibold text-700 mb-1 block">Tipo Sanguíneo</label>
            <Dropdown id="bloodType" v-model="bloodType" :options="['A+', 'A-', 'B+', 'B-', 'AB+', 'AB-', 'O+', 'O-', 'Desconhecido']" placeholder="Selecione" />
          </div>

          <div class="col-12 md:col-8 mb-2">
            <label for="allergies" class="font-semibold text-red-600 mb-1 block"><i class="pi pi-exclamation-triangle mr-1"></i>Alergias Conhecidas</label>
            <InputText id="allergies" v-model="allergies" placeholder="Ex: Penicilina, Dipirona, Frutos do mar, Látex" />
          </div>

          <div class="col-12 md:col-6 mb-2">
            <label for="chronicDiseases" class="font-semibold text-700 mb-1 block">Doenças Crônicas</label>
            <InputText id="chronicDiseases" v-model="chronicDiseases" placeholder="Ex: Hipertensão, Diabetes tipo 2, Asma" />
          </div>

          <div class="col-12 md:col-6 mb-2">
            <label for="continuousMedications" class="font-semibold text-700 mb-1 block">Medicamentos de Uso Contínuo</label>
            <InputText id="continuousMedications" v-model="continuousMedications" placeholder="Ex: Losartana 50mg, Metformina 850mg" />
          </div>

          <div class="col-12 mb-2">
            <label for="medicalAlerts" class="font-semibold text-orange-600 mb-1 block"><i class="pi pi-bell mr-1"></i>Alertas Clínicos & Recomendações</label>
            <InputText id="medicalAlerts" v-model="medicalAlerts" placeholder="Ex: Gestante, Portador de Marcapasso, Histórico de Quelóide" />
          </div>
        </div>
      </div>
    </form>
    <template #footer>
      <Button label="Cancelar" icon="pi pi-times" class="p-button-text" @click="closeModal"/>
      <Button :label="isEditMode ? 'Salvar Alterações' : 'Cadastrar Paciente'" icon="pi pi-check" class="p-button-success" @click="onSubmit" autofocus />
    </template>
  </Dialog>
</template>

<script setup>
import { ref, watch } from 'vue';
import { useForm, useField } from 'vee-validate';
import { toTypedSchema } from '@vee-validate/zod';
import * as z from 'zod';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Calendar from 'primevue/calendar';
import Dropdown from 'primevue/dropdown';

const props = defineProps({
  visible: Boolean,
  patientData: Object,
});

const emit = defineEmits(['close', 'save']);

const isModalVisible = ref(props.visible);
const isEditMode = ref(false);

const validationSchema = toTypedSchema(
  z.object({
    name: z.string().nonempty('Nome é obrigatório').min(3, 'Mínimo 3 caracteres'),
    email: z.string().nonempty('E-mail é obrigatório').email('E-mail inválido'),
    phone: z.string().max(30, 'Máximo de 30 caracteres').optional(),
    cpf: z.string().nonempty('CPF é obrigatório'),
    birthDate: z.any().refine(val => !!val, 'Data de nascimento é obrigatória')
  })
);

const { handleSubmit, errors, setValues, resetForm } = useForm({
  validationSchema
});

const { value: name } = useField('name');
const { value: email } = useField('email');
const { value: phone } = useField('phone');
const { value: cpf } = useField('cpf');
const { value: birthDate } = useField('birthDate');

// Additional fields (managed via refs)
const socialName = ref('');
const gender = ref('');
const address = ref('');
const profession = ref('');
const emergencyContact = ref('');
const legalGuardian = ref('');
const marketingOrigin = ref('');
const bloodType = ref('');
const allergies = ref('');
const chronicDiseases = ref('');
const continuousMedications = ref('');
const medicalAlerts = ref('');

watch(() => props.visible, (value) => {
  isModalVisible.value = value;
  if (value) {
    if (props.patientData) {
      isEditMode.value = true;
      setValues({
        id: props.patientData.id,
        name: props.patientData.name,
        email: props.patientData.email,
        phone: props.patientData.phone || '',
        cpf: props.patientData.cpf,
        birthDate: props.patientData.birthDate ? new Date(props.patientData.birthDate) : new Date()
      });
      socialName.value = props.patientData.socialName || '';
      gender.value = props.patientData.gender || '';
      address.value = props.patientData.address || '';
      profession.value = props.patientData.profession || '';
      emergencyContact.value = props.patientData.emergencyContact || '';
      legalGuardian.value = props.patientData.legalGuardian || '';
      marketingOrigin.value = props.patientData.marketingOrigin || '';
      bloodType.value = props.patientData.bloodType || '';
      allergies.value = props.patientData.allergies || '';
      chronicDiseases.value = props.patientData.chronicDiseases || '';
      continuousMedications.value = props.patientData.continuousMedications || '';
      medicalAlerts.value = props.patientData.medicalAlerts || '';
    } else {
      isEditMode.value = false;
      resetForm();
      socialName.value = '';
      gender.value = '';
      address.value = '';
      profession.value = '';
      emergencyContact.value = '';
      legalGuardian.value = '';
      marketingOrigin.value = '';
      bloodType.value = '';
      allergies.value = '';
      chronicDiseases.value = '';
      continuousMedications.value = '';
      medicalAlerts.value = '';
    }
  }
});

const closeModal = () => {
  emit('close');
};

const onSubmit = handleSubmit((values) => {
  emit('save', {
    ...values,
    socialName: socialName.value,
    gender: gender.value,
    address: address.value,
    profession: profession.value,
    emergencyContact: emergencyContact.value,
    legalGuardian: legalGuardian.value,
    marketingOrigin: marketingOrigin.value,
    bloodType: bloodType.value,
    allergies: allergies.value,
    chronicDiseases: chronicDiseases.value,
    continuousMedications: continuousMedications.value,
    medicalAlerts: medicalAlerts.value
  });
});
</script>
