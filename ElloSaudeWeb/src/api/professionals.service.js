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
  }
};
