import apiClient from "./axios";

export const medicalRecordsService = {
  async getRecordsByPatient(patientId) {
    const response = await apiClient.get(`/medicalrecords/patient/${patientId}`);
    return response.data;
  },

  async createRecord(recordData) {
    const response = await apiClient.post('/medicalrecords', recordData);
    return response.data;
  },

  async addAddendum(id, note) {
    const response = await apiClient.post(`/medicalrecords/${id}/addenda`, { note });
    return response.data;
  }
};
