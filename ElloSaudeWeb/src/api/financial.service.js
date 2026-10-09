import apiClient from "./axios";

export const financialService = {
  async getSummary(startDate, endDate) {
    const params = {};
    if (startDate) params.startDate = startDate;
    if (endDate) params.endDate = endDate;
    const response = await apiClient.get('/financial/summary', { params });
    return response.data;
  }
};
