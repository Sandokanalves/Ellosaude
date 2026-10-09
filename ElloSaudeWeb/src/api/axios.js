import axios from 'axios';

const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_URL || (import.meta.env.DEV ? 'http://localhost:5080/api' : '/api'),
  headers: {
    'Content-Type': 'application/json',
  },
});

apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('token');
    if (token) {
      config.headers['Authorization'] = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response && error.response.status === 401) {
      localStorage.removeItem('token');
      localStorage.removeItem('user');
      if (window.location.pathname !== '/auth/login') {
        window.location.href = '/auth/login';
      }
    }
    if (error.response?.status === 403) {
      const user = JSON.parse(localStorage.getItem('user') || 'null');
      if (user?.mustChangePassword && window.location.pathname !== '/auth/change-password') {
        window.location.href = '/auth/change-password';
      }
    }
    return Promise.reject(error);
  }
);

export default apiClient;
