import apiClient from "./axios";

export const appointmentsService = {
  async getAppointments(startDate, endDate) {
    const params = {};
    if (startDate) params.startDate = startDate;
    if (endDate) params.endDate = endDate;
    const response = await apiClient.get('/appointments', { params });
    return response.data;
  },

  async getWaitingRoom() {
    const response = await apiClient.get('/appointments/waiting-room');
    return response.data;
  },

  async getWhatsAppConfirmationLink(id, timeZoneId) {
    const response = await apiClient.get(`/appointments/${id}/whatsapp-confirmation-link`, {
      params: { timeZoneId }
    });
    return response.data;
  },

  async getStatusHistory(id) {
    const response = await apiClient.get(`/appointments/${id}/status-history`);
    return response.data;
  },

  async createAppointment(appointmentData) {
    const response = await apiClient.post('/appointments', appointmentData);
    return response.data;
  },

  async updateStatus(id, status) {
    const response = await apiClient.patch(`/appointments/${id}/status`, { id, status });
    return response.data;
  },

  async deleteAppointment(id) {
    const response = await apiClient.delete(`/appointments/${id}`);
    return response.data;
  }
};
