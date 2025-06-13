Page({
  data: {
    username: '',
    password: '',
    confirmPassword: ''
  },

  onUsernameInput(e) {
    this.setData({ username: e.detail.value });
  },

  onPasswordInput(e) {
    this.setData({ password: e.detail.value });
  },

  onConfirmPasswordInput(e) {
    this.setData({ confirmPassword: e.detail.value });
  },

  goToLogin() {
    wx.navigateBack();
  },

  async onRegister() {
    const { username, password, confirmPassword } = this.data;
    
    if (!username || !password || !confirmPassword) {
      wx.showToast({ title: '请填写完整信息', icon: 'none' });
      return;
    }
    
    if (password !== confirmPassword) {
      wx.showToast({ title: '两次密码不一致', icon: 'none' });
      return;
    }
    
      wx.request({
        url: 'https://127.0.0.1:7024/api/Auth/Register',
        method: 'POST',
        data: {
          Username: username,
          Password: password,
          Password2:confirmPassword
        },
        success:res=>{
          wx.showToast({ title: '注册成功' });
          setTimeout(() => {
           wx.navigateBack(); // 返回登录页
          }, 1500);
        },
        fail:err=>{
          wx.showToast({ 
            title: res.data.message || '注册失败',
            icon: 'none'
          });
        }
      });
  }
});