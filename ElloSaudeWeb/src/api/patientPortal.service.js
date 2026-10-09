import apiClient from "./axios";

export const patientPortalService = {
  async getAppointments() {
    const response = await apiClient.get('/patient-portal/my-appointments');
    return response.data;
  },

  async getProfessionals() {
    const response = await apiClient.get('/professionals');
    return response.data;
  },

  async getAvailability(professionalId, date) {
    const response = await apiClient.get('/patient-portal/availability', {
      params: { professionalId, date }
    });
    return response.data;
  },

  async bookAppointment(booking) {
    const response = await apiClient.post('/patient-portal/book', booking);
    return response.data;
  },

  async cancelAppointment(id) {
    await apiClient.post(`/patient-portal/cancel/${id}`);
  },

  async getPrescriptions() {
    const response = await apiClient.get('/patient-portal/my-prescriptions');
    return response.data;
  },

  async downloadPrescription(id) {
    const response = await apiClient.get(`/patient-portal/my-prescriptions/${id}/pdf`, {
      responseType: 'blob'
    });
    return response.data;
  }
};
