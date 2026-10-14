import apiClient from './axios';

export const professionalsService = {
  async getAll() {
    const response = await apiClient.get('/professionals');
    return response.data;
  },

  async getAvailableSlots(professionalId, date) {
    const response = await apiClient.get(`/professionals/${professionalId}/available-slots`, {
      params: { date }
    });
    return response.data;
  },

  async getScheduleBlocks(startDateTime, endDateTime, professionalId) {
    const response = await apiClient.get('/professionals/blocks', {
      params: { startDateTime, endDateTime, professionalId }
    });
    return response.data;
  },

  async createScheduleBlock(professionalId, block) {
    const response = await apiClient.post(`/professionals/${professionalId}/blocks`, block);
    return response.data;
  },

  async cancelScheduleBlock(professionalId, blockId) {
    const response = await apiClient.delete(`/professionals/${professionalId}/blocks/${blockId}`);
    return response.data;
  }
};
