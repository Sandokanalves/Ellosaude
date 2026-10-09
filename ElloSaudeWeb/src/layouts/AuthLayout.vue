<template>
  <div class="auth-layout">
    <router-view />
  </div>
</template>

<script setup>
// No logic needed for a simple layout
</script>

<style scoped>
.auth-layout {
  display: flex;
  justify-content: center;
  align-items: center;
  min-height: 100vh;
  background-color: #f4f7f5;
}
</style>

