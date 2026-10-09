import apiClient from './axios';

export const prescriptionsService = {
  async create(prescription) {
    const response = await apiClient.post('/prescriptions', prescription);
    return response.data;
  },

  async downloadPdf(id) {
    const response = await apiClient.get(`/prescriptions/${id}/pdf`, {
      responseType: 'blob'
    });
    return response.data;
  }
};
