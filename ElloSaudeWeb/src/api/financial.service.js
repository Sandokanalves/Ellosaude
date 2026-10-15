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
  },

  async getExpenses(startDate = null, endDate = null, category = null) {
    const params = {};
    if (startDate) params.startDate = startDate;
    if (endDate) params.endDate = endDate;
    if (category) params.category = category;
    const response = await apiClient.get('/financial/expenses', { params });
    return response.data;
  },

  async createExpense(expense) {
    const response = await apiClient.post('/financial/expenses', expense);
    return response.data;
  },

  async payExpense(id, data = {}) {
    await apiClient.patch(`/financial/expenses/${id}/pay`, data);
  },

  async deleteExpense(id) {
    await apiClient.delete(`/financial/expenses/${id}`);
  },

  async getCurrentCashRegister() {
    const response = await apiClient.get('/financial/cash-register/current');
    return response.data;
  },

  async openCashRegister(openingBalance, notes = null) {
    const response = await apiClient.post('/financial/cash-register/open', { openingBalance, notes });
    return response.data;
  },

  async closeCashRegister(closingBalance, notes = null) {
    const response = await apiClient.post('/financial/cash-register/close', { closingBalance, notes });
    return response.data;
  },

  async getDre(startDate = null, endDate = null) {
    const params = {};
    if (startDate) params.startDate = startDate;
    if (endDate) params.endDate = endDate;
    const response = await apiClient.get('/financial/dre', { params });
    return response.data;
  }
};
