import { createRouter, createWebHistory } from "vue-router";
import { useAuthStore } from "@/stores/auth.store";

import DefaultLayout from "@/layouts/DefaultLayout.vue";
import AuthLayout from "@/layouts/AuthLayout.vue";

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: "/",
      component: DefaultLayout,
      meta: { requiresAuth: true },
      children: [
        {
          path: "",
          name: "agenda",
          component: () => import("@/pages/agenda/AgendaPage.vue"),
        },
        {
          path: "pacientes",
          name: "pacientes",
          component: () => import("@/pages/patients/PatientsListPage.vue"),
        },
        {
          path: "pacientes/:id",
          name: "paciente-detalhe",
          component: () => import("@/pages/patients/PatientDetailPage.vue"),
          props: true,
        },
        {
          path: "financeiro",
          name: "financeiro",
          component: () => import("@/pages/FinancialPage.vue"),
        },
        {
          path: "configuracoes",
          name: "config",
          component: () => import("@/pages/SettingsPage.vue"),
        },
      ],
    },
    {
      path: "/auth",
      component: AuthLayout,
      children: [
        {
          path: "login",
          name: "login",
          component: () => import("@/pages/LoginPage.vue"),
        },
      ],
    },
  ],
});

router.beforeEach((to, from, next) => {
  const authStore = useAuthStore();
  if (to.meta.requiresAuth && !authStore.isAuthenticated) {
    next({ name: "login" });
  } else {
    next();
  }
});

export default router;

