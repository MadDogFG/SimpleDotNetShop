Page({
  data: {
    username: '',
    password: ''
  },

  onUsernameInput(e) {
    this.setData({ username: e.detail.value });
  },

  onPasswordInput(e) {
    this.setData({ password: e.detail.value });
  },

  goToRegister() {
    wx.navigateTo({ url: '/pages/register/register' });
  },

  async onLogin() {
    const { username, password } = this.data;
    
    if (!username || !password) {
      wx.showToast({ title: '请填写完整信息', icon: 'none' });
      return;
    }
    
      // 调用后端登录API
      wx.request({
        url: 'https://localhost:7024/api/Auth/Login',
        method: 'POST',
        data: {
          Username: username,
          Password: password
        },
        success:res=>{
          console.log('登录成功',res);
          wx.showToast({ title: res.data.message || '登录成功', icon: 'none' });
          // 存储token和用户信息
          wx.setStorageSync('token', res.data.token);
          wx.setStorageSync('userInfo', {
          username: res.data.username,
          roles: res.data.roles
          });
          // 根据角色跳转不同页面
          if (res.data.roles.includes('Admin')) {
            wx.showToast({
              title: '跳转管理页面',
            })
            wx.redirectTo({ url: '/pages/admin/admin' });
          } else {
            console.log("跳转用户首页");
            wx.switchTab({ url: '/pages/products/products' });
          }
        },
        fail:err=>{
          console.log('登录失败',err)
          wx.showToast({ title: res.data.message || '登录失败', icon: 'none' });
        }
      });
  }
});