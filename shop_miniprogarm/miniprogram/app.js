App({
  onLaunch: function () {
    console.log('App Launch');
    // 可以在此处进行一些初始化操作，比如登录、获取用户信息等
  },
  onShow: function () {
    console.log('App Show');
    // 应用进入前台时触发，可以进行一些页面刷新操作
  },
  onHide: function () {
    console.log('App Hide');
    // 应用进入后台时触发，可以进行一些数据保存操作
  },
  globalData: {
    userInfo: null
  }
});

