<template>
  <div class="login-container">
    <Card class="login-card p-shadow-6">
      <template #title>
        <div class="text-center mb-4">
          <i class="pi pi-heart-fill text-primary text-4xl mb-2"></i>
          <h2 class="text-3xl font-bold text-900 m-0">ElloSaúde</h2>
          <p class="text-500 text-sm mt-1">Sistema de Gestão de Clínicas</p>
        </div>
      </template>
      <template #content>
        <Message v-if="errorMessage" severity="error" :closable="false" class="mb-4">{{ errorMessage }}</Message>

        <form @submit.prevent="onSubmit">
          <div class="p-fluid">
            <div class="p-field mb-3">
              <label for="email" class="font-semibold text-700">E-mail</label>
              <InputText id="email" v-model="email" placeholder="admin@ellosaude.com" :class="{ 'p-invalid': errors.email }" />
              <small class="p-error">{{ errors.email }}</small>
            </div>
            <div class="p-field mb-4">
              <label for="password" class="font-semibold text-700">Senha</label>
              <Password id="password" v-model="password" placeholder="••••••••" :feedback="false" toggleMask :class="{ 'p-invalid': errors.password }" />
              <small class="p-error">{{ errors.password }}</small>
            </div>
          </div>
          <Button type="submit" label="Entrar no Sistema" icon="pi pi-sign-in" class="p-button-primary p-button-raised w-full py-3 text-lg font-bold" :loading="loading" />
        </form>

        <div class="demo-credentials mt-4 p-3 border-round bg-blue-50 border-1 border-blue-200">
          <p class="text-xs text-blue-900 font-bold m-0 mb-1"><i class="pi pi-info-circle mr-1"></i> Credenciais de Demonstração:</p>
          <p class="text-xs text-blue-800 m-0">E-mail: <strong>admin@ellosaude.com</strong> | Senha: <strong>Senha123!</strong></p>
        </div>
      </template>
    </Card>
  </div>
</template>

<script setup>
import { ref } from 'vue';
import { useForm, useField } from 'vee-validate';
import { toTypedSchema } from '@vee-validate/zod';
import * as z from 'zod';
import { useAuthStore } from '@/stores/auth.store';
import Card from 'primevue/card';
import InputText from 'primevue/inputtext';
import Password from 'primevue/password';
import Button from 'primevue/button';
import Message from 'primevue/message';

const authStore = useAuthStore();
const errorMessage = ref('');
const loading = ref(false);

const validationSchema = toTypedSchema(
  z.object({
    email: z.string().nonempty('O e-mail é obrigatório').email('Informe um e-mail válido'),
    password: z.string().nonempty('A senha é obrigatória').min(6, 'A senha deve ter no mínimo 6 caracteres'),
  })
);

const { handleSubmit, errors } = useForm({
  validationSchema,
  initialValues: {
    email: 'admin@ellosaude.com',
    password: 'Senha123!'
  }
});

const { value: email } = useField('email');
const { value: password } = useField('password');

const onSubmit = handleSubmit(async (values) => {
  errorMessage.value = '';
  loading.value = true;
  try {
    await authStore.login(values.email, values.password);
  } catch (err) {
    errorMessage.value = err.message || 'Credenciais inválidas. Tente novamente.';
  } finally {
    loading.value = false;
  }
});
</script>

<style scoped>
.login-container {
  width: 100vw;
  height: 100vh;
  display: flex;
  justify-content: center;
  align-items: center;
  background: linear-gradient(135deg, #eef2f3 0%, #8e9eab 100%);
}
.login-card {
  width: 100%;
  max-width: 420px;
  border-radius: 12px;
}
.demo-credentials {
  border-radius: 8px;
}
</style>
