<template>
  <Dialog :header="isEditMode ? 'Editar Paciente' : 'Novo Paciente'" v-model:visible="isModalVisible" :modal="true" :style="{width: '500px'}" @hide="closeModal">
    <form @submit.prevent="onSubmit">
      <div class="p-fluid">
        <div class="p-field mb-3">
          <label for="name" class="font-semibold text-700 mb-1 block">Nome Completo</label>
          <InputText id="name" v-model="name" placeholder="Ex: Maria Santos" :class="{ 'p-invalid': errors.name }" />
          <small class="p-error">{{ errors.name }}</small>
        </div>

        <div class="p-field mb-3">
          <label for="email" class="font-semibold text-700 mb-1 block">E-mail</label>
          <InputText id="email" v-model="email" placeholder="maria@email.com" :class="{ 'p-invalid': errors.email }" />
          <small class="p-error">{{ errors.email }}</small>
        </div>

        <div class="p-field mb-3">
          <label for="cpf" class="font-semibold text-700 mb-1 block">CPF</label>
          <InputText id="cpf" v-model="cpf" placeholder="000.000.000-00" :class="{ 'p-invalid': errors.cpf }" />
          <small class="p-error">{{ errors.cpf }}</small>
        </div>

        <div class="p-field mb-3">
          <label for="birthDate" class="font-semibold text-700 mb-1 block">Data de Nascimento</label>
          <Calendar id="birthDate" v-model="birthDate" dateFormat="dd/mm/yy" showIcon placeholder="dd/mm/aaaa" :class="{ 'p-invalid': errors.birthDate }" />
          <small class="p-error">{{ errors.birthDate }}</small>
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
    cpf: z.string().nonempty('CPF é obrigatório'),
    birthDate: z.any().refine(val => !!val, 'Data de nascimento é obrigatória')
  })
);

const { handleSubmit, errors, setValues, resetForm } = useForm({
  validationSchema
});

const { value: name } = useField('name');
const { value: email } = useField('email');
const { value: cpf } = useField('cpf');
const { value: birthDate } = useField('birthDate');

watch(() => props.visible, (value) => {
  isModalVisible.value = value;
  if (value) {
    if (props.patientData) {
      isEditMode.value = true;
      setValues({
        id: props.patientData.id,
        name: props.patientData.name,
        email: props.patientData.email,
        cpf: props.patientData.cpf,
        birthDate: props.patientData.birthDate ? new Date(props.patientData.birthDate) : new Date()
      });
    } else {
      isEditMode.value = false;
      resetForm();
    }
  }
});

const closeModal = () => {
  emit('close');
};

const onSubmit = handleSubmit((values) => {
  const bDate = values.birthDate instanceof Date ? values.birthDate.toISOString() : new Date(values.birthDate).toISOString();
  emit('save', {
    ...values,
    id: props.patientData?.id,
    birthDate: bDate
  });
});
</script>
