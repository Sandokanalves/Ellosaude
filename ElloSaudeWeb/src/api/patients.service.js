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
  },

  async createPortalAccount(id) {
    const response = await apiClient.post(`/patients/${id}/portal-account`);
    return response.data;
  },

  async updateClinicalSummary(id, summaryData) {
    const response = await apiClient.patch(`/patients/${id}/clinical-summary`, { ...summaryData, patientId: id });
    return response.data;
  },

  async getMarkers(patientId, target = null) {
    const params = target ? { target } : {};
    const response = await apiClient.get(`/patients/${patientId}/markers`, { params });
    return response.data;
  },

  async saveMarker(patientId, markerData) {
    const response = await apiClient.post(`/patients/${patientId}/markers`, { ...markerData, patientId });
    return response.data;
  },

  async deleteMarker(patientId, markerId) {
    const response = await apiClient.delete(`/patients/${patientId}/markers/${markerId}`);
    return response.data;
  },

  async getAttachments(patientId, category = null) {
    const params = category ? { category } : {};
    const response = await apiClient.get(`/patients/${patientId}/attachments`, { params });
    return response.data;
  },

  async createAttachment(patientId, attachmentData) {
    const response = await apiClient.post(`/patients/${patientId}/attachments`, { ...attachmentData, patientId });
    return response.data;
  },

  async deleteAttachment(patientId, attachmentId) {
    const response = await apiClient.delete(`/patients/${patientId}/attachments/${attachmentId}`);
    return response.data;
  }
};
