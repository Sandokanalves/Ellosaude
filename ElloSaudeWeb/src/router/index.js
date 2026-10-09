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
          meta: { roles: ["Admin", "Profissional", "Secretaria"] },
          component: () => import("@/pages/agenda/AgendaPage.vue"),
        },
        {
          path: "pacientes",
          name: "pacientes",
          meta: { roles: ["Admin", "Profissional", "Secretaria"] },
          component: () => import("@/pages/patients/PatientsListPage.vue"),
        },
        {
          path: "pacientes/:id",
          name: "paciente-detalhe",
          meta: { roles: ["Admin", "Profissional", "Secretaria"] },
          component: () => import("@/pages/patients/PatientDetailPage.vue"),
          props: true,
        },
        {
          path: "financeiro",
          name: "financeiro",
          meta: { roles: ["Admin", "Profissional", "Secretaria"] },
          component: () => import("@/pages/FinancialPage.vue"),
        },
        {
          path: "configuracoes",
          name: "config",
          meta: { roles: ["Admin", "Profissional", "Secretaria"] },
          component: () => import("@/pages/SettingsPage.vue"),
        },
        {
          path: "portal",
          name: "patient-portal",
          meta: { roles: ["Paciente"] },
          component: () => import("@/pages/PatientPortalPage.vue"),
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
        {
          path: "change-password",
          name: "change-password",
          meta: { requiresAuth: true, allowPasswordChange: true },
          component: () => import("@/pages/ChangePasswordPage.vue"),
        },
      ],
    },
  ],
});

router.beforeEach((to) => {
  const authStore = useAuthStore();
  if (to.meta.requiresAuth && !authStore.isAuthenticated) return { name: "login" };
  if (authStore.isAuthenticated && authStore.mustChangePassword && !to.meta.allowPasswordChange)
    return { name: "change-password" };
  if (to.meta.roles && !to.meta.roles.includes(authStore.user?.role))
    return authStore.isPatient ? { name: "patient-portal" } : { name: "agenda" };
  if (to.name === "login" && authStore.isAuthenticated)
    return authStore.mustChangePassword
      ? { name: "change-password" }
      : authStore.isPatient ? { name: "patient-portal" } : { name: "agenda" };
});

export default router;
