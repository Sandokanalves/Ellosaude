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

  async updateRecord(id, recordData) {
    const response = await apiClient.put(`/medicalrecords/${id}`, { ...recordData, id });
    return response.data;
  },

  async deleteRecord(id) {
    const response = await apiClient.delete(`/medicalrecords/${id}`);
    return response.data;
  }
};
