import apiClient from './axios';

export const notificationsService = {
  async getRecent() {
    const response = await apiClient.get('/notifications');
    return response.data;
  },

  async markRead(id) {
    await apiClient.patch(`/notifications/${id}/read`);
  }
};
