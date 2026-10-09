import { createApp } from 'vue'
import { createPinia } from 'pinia'

import App from './App.vue'
import router from '@/router'

// PrimeVue
import PrimeVue from 'primevue/config'
import ToastService from 'primevue/toastservice';
import 'primevue/resources/themes/saga-blue/theme.css' // Tema
import 'primevue/resources/primevue.min.css' // Core CSS
import 'primeicons/primeicons.css' // Ícones
import 'primeflex/primeflex.css'; // Utilitários de layout

import '@/assets/main.css'

const app = createApp(App)

app.use(createPinia())
app.use(router)
app.use(PrimeVue, { ripple: true })
app.use(ToastService);

app.mount('#app')
