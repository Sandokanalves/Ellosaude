<template>
  <aside class="layout-sidebar">
    <nav class="menu">
      <ul>
        <li v-for="item in menuItems" :key="item.label">
          <router-link :to="item.to" class="menu-item" active-class="active">
            <i :class="item.icon"></i>
            <span>{{ item.label }}</span>
          </router-link>
        </li>
      </ul>
    </nav>
    <div class="logout-section">
      <a @click.prevent="logout" class="menu-item">
        <i class="pi pi-sign-out"></i>
        <span>Sair</span>
      </a>
    </div>
  </aside>
</template>

<script setup>
import { computed } from "vue";
import { useAuthStore } from "@/stores/auth.store";

const authStore = useAuthStore();

const menuItems = computed(() => authStore.isPatient
  ? [{ label: "Meu portal", icon: "pi pi-user", to: { name: "patient-portal" } }]
  : [
      { label: "Agenda", icon: "pi pi-calendar", to: { name: "agenda" } },
      { label: "Pacientes", icon: "pi pi-users", to: { name: "pacientes" } },
      { label: "Financeiro", icon: "pi pi-dollar", to: { name: "financeiro" } },
      { label: "Configurações", icon: "pi pi-cog", to: { name: "config" } },
    ]);

function logout() {
  authStore.logout();
}
</script>

<style scoped>
.layout-sidebar {
  width: 250px;
  background-color: #264653; /* Cor escura do protótipo */
  color: #ffffff;
  display: flex;
  flex-direction: column;
  height: 100vh;
  position: fixed;
  left: 0;
  top: 60px; /* Abaixo do Header */
  bottom: 0;
}

.menu {
  flex-grow: 1;
  margin-top: 1rem;
}

.menu ul {
  list-style: none;
  padding: 0;
  margin: 0;
}

.menu-item {
  display: flex;
  align-items: center;
  padding: 1rem 1.5rem;
  color: #ffffff;
  text-decoration: none;
  transition: background-color 0.2s;
  cursor: pointer;
}

.menu-item:hover {
  background-color: #4a6b79;
}

.menu-item.active {
  background-color: #2A9D8F; /* Cor primária */
  font-weight: bold;
}

.menu-item i {
  margin-right: 1rem;
  font-size: 1.2rem;
}

.logout-section {
  padding: 1rem;
  border-top: 1px solid #4a6b79;
}
</style>
