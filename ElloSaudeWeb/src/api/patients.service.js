import apiClient from "./axios";

export const patientsService = {
  async getAllPatients() {
    const response = await apiClient.get('/patients');
    return response.data;
  },

  async getPatientById(id) {
    const response = await apiClient.get(`/patients/${id}`);
    return response.data;
  },

  async createPatient(patientData) {
    const response = await apiClient.post('/patients', patientData);
    return response.data;
  },

  async updatePatient(id, patientData) {
    const response = await apiClient.put(`/patients/${id}`, { ...patientData, id });
    return response.data;
  },

  async deletePatient(id) {
    const response = await apiClient.delete(`/patients/${id}`);
    return response.data;
  }
};
