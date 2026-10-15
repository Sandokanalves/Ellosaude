import api from './axios';

export const usersService = {
  async getStaff() {
    const { data } = await api.get('/users/staff');
    return data;
  },

  async createStaff(staffData) {
    const { data } = await api.post('/users/staff', staffData);
    return data;
  },

  async toggleStaffStatus(id) {
    await api.patch(`/users/staff/${id}/toggle-status`);
  }
};
