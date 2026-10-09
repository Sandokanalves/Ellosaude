import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { useRouter } from 'vue-router';
import apiClient from '@/api/axios';

function parseJwt(token) {
  try {
    const base64Url = token.split('.')[1];
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
    const jsonPayload = decodeURIComponent(
      atob(base64)
        .split('')
        .map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
        .join('')
    );
    return JSON.parse(jsonPayload);
  } catch (e) {
    return null;
  }
}

export const useAuthStore = defineStore('auth', () => {
  const router = useRouter();

  const user = ref(JSON.parse(localStorage.getItem('user')) || null);
  const token = ref(localStorage.getItem('token') || null);

  const isAuthenticated = computed(() => !!token.value);
  const isDoctor = computed(() => user.value?.role === 'Profissional');
  const isPatient = computed(() => user.value?.role === 'Paciente');
  const mustChangePassword = computed(() => user.value?.mustChangePassword === true);

  async function login(email, password) {
    try {
      const response = await apiClient.post('/auth/login', { email, password });
      const authToken = response.data.token || response.data.Token;

      const payload = parseJwt(authToken);
      const userData = {
        id: payload?.['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] || payload?.nameid || '',
        email: payload?.['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] || email,
        role: payload?.['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role'] || payload?.role || '',
        mustChangePassword: payload?.MustChangePassword === 'true',
        tenantId: payload?.['TenantId'] || ''
      };

      user.value = userData;
      token.value = authToken;

      localStorage.setItem('user', JSON.stringify(userData));
      localStorage.setItem('token', authToken);

      router.push(userData.mustChangePassword
        ? { name: 'change-password' }
        : userData.role === 'Paciente' ? { name: 'patient-portal' } : { name: 'agenda' });
      return { success: true };
    } catch (error) {
      const message = error.response?.data?.message || 'Falha ao autenticar. Verifique e-mail e senha.';
      throw new Error(message);
    }
  }

  function logout() {
    user.value = null;
    token.value = null;
    localStorage.removeItem('user');
    localStorage.removeItem('token');
    router.push({ name: 'login' });
  }

  async function changePassword(currentPassword, newPassword) {
    await apiClient.post('/auth/change-password', { currentPassword, newPassword });
    logout();
  }

  return { user, token, isAuthenticated, isDoctor, isPatient, mustChangePassword, login, logout, changePassword };
});
