<template>
  <div class="layout-wrapper">
    <TheHeader />
    <TheSidebar />
    <main class="layout-main">
      <RouterView />
    </main>
  </div>
</template>

<script setup>
import TheSidebar from "@/components/TheSidebar.vue";
import TheHeader from "@/components/TheHeader.vue";
</script>

<style scoped>
.layout-wrapper {
  display: flex;
}

.layout-main {
  flex-grow: 1;
  padding: 1.5rem;
  margin-top: 60px; /* Altura do Header */
  margin-left: 250px; /* Largura da Sidebar */
}
</style>

