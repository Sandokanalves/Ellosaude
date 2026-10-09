<template>
  <header class="layout-header">
    <div class="logo">
      <h2>Ello Saúde</h2>
    </div>
    <div class="header-content">
      <span class="p-input-icon-left">
          <i class="pi pi-search" />
          <InputText placeholder="Buscar paciente..." />
      </span>
      <i class="pi pi-bell p-overlay-badge" v-badge.danger></i>
      <div class="user-profile">
        <Avatar icon="pi pi-user" class="mr-2" />
        <span>Dra. Ana Sousa</span>
      </div>
    </div>
  </header>
</template>

<script setup>
import InputText from 'primevue/inputtext';
import Avatar from 'primevue/avatar';
</script>

<style scoped>
.layout-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1rem 1.5rem;
  background-color: #ffffff;
  border-bottom: 1px solid #e0e0e0;
}
.header-content {
  display: flex;
  align-items: center;
  gap: 1.5rem;
}
.user-profile {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}
</style>

