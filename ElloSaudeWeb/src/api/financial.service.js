import apiClient from "./axios";

export const financialService = {
  async getSummary(startDate, endDate) {
    const params = {};
    if (startDate) params.startDate = startDate;
    if (endDate) params.endDate = endDate;
    const response = await apiClient.get('/financial/summary', { params });
    return response.data;
  },

  async getReport(startDate, endDate, professionalId) {
    const params = {};
    if (startDate) params.startDate = startDate;
    if (endDate) params.endDate = endDate;
    if (professionalId) params.professionalId = professionalId;
    const response = await apiClient.get('/financial/report', { params });
    return response.data;
  },

  async getPendingPayments(startDate, endDate) {
    const params = {};
    if (startDate) params.startDate = startDate;
    if (endDate) params.endDate = endDate;
    const response = await apiClient.get('/financial/pending', { params });
    return response.data;
  },

  async registerPayment(paymentRecordId, payment) {
    const response = await apiClient.post(`/financial/${paymentRecordId}/pay`, payment);
    return response.data;
  },

  async cancelPayment(paymentRecordId, reason) {
    const response = await apiClient.post(`/financial/${paymentRecordId}/cancel`, { reason });
    return response.data;
  }
};
