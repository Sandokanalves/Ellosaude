<template>
  <main class="password-page">
    <form class="password-card" @submit.prevent="submit">
      <h1>Defina uma senha pessoal</h1>
      <p>Por segurança, a senha temporária não pode ser usada para acessar o portal.</p>
      <label>
        Senha temporária ou atual
        <input v-model="currentPassword" type="password" autocomplete="current-password" required />
      </label>
      <label>
        Nova senha (mínimo de 16 caracteres)
        <input v-model="newPassword" type="password" autocomplete="new-password" minlength="16" required />
      </label>
      <label>
        Confirme a nova senha
        <input v-model="confirmation" type="password" autocomplete="new-password" minlength="16" required />
      </label>
      <p v-if="error" class="error-message" role="alert">{{ error }}</p>
      <Button label="Alterar senha" type="submit" :loading="loading" />
      <small>Após a alteração, entre novamente com a senha escolhida.</small>
    </form>
  </main>
</template>

<script setup>
import { ref } from 'vue';
import { useAuthStore } from '@/stores/auth.store';
import Button from 'primevue/button';

const authStore = useAuthStore();
const currentPassword = ref('');
const newPassword = ref('');
const confirmation = ref('');
const error = ref('');
const loading = ref(false);

async function submit() {
  error.value = '';
  if (newPassword.value.length < 16) {
    error.value = 'A nova senha deve conter ao menos 16 caracteres.';
    return;
  }
  if (newPassword.value !== confirmation.value) {
    error.value = 'As senhas não coincidem.';
    return;
  }

  loading.value = true;
  try {
    await authStore.changePassword(currentPassword.value, newPassword.value);
  } catch (err) {
    error.value = err.response?.data?.detail || err.message || 'Não foi possível alterar a senha.';
  } finally {
    loading.value = false;
  }
}
</script>

<style scoped>
.password-page {
  min-height: 100vh;
  display: grid;
  place-items: center;
  padding: 1rem;
  background: #f3f7f8;
}
.password-card {
  width: min(100%, 440px);
  display: grid;
  gap: 1rem;
  padding: 2rem;
  background: white;
  border-radius: 12px;
  box-shadow: 0 8px 28px #163e4a1a;
}
.password-card h1,
.password-card p {
  margin: 0;
}
.password-card label {
  display: grid;
  gap: 0.4rem;
}
.password-card input {
  padding: 0.7rem;
  border: 1px solid #bdcdd2;
  border-radius: 6px;
}
.error-message {
  color: #b42318;
}
</style>
