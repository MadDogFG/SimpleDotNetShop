const API_BASE_URL = 'https://localhost:7024/api';

Page({
  data: {
    isLoggedIn: false,
    userInfo: { // 从缓存或API获取
      username: '',
      roles: []
    },
    showAddressModal: false,
    addresses: [],        // 用户地址列表
    addressLoading: false,
    isEditingAddress: false, // 是否处于编辑地址表单状态
    isAddingAddress: false,  // 是否处于新增地址表单状态
    addressFormData: {     // 地址表单数据
      id: null,
      contactName: '',
      phoneNumber: '',
      province: '',
      city: '',
      streetAddress: '',
      postalCode: '',
      isDefault: false
    },
    region: ['请选择', '请选择', '请选择'], // 用于地区选择器
    customItem: '请选择',
    savingAddress: false, // 是否正在保存地址

    showDeleteAddressConfirmModal: false,
    addressToDelete: { id: null, name: '' },
    deletingAddress: false,

    // for checkout page selection (如果从结算页来选择地址)
    fromCheckout: false,
    selectedAddressForCheckout: null,
  },

  onLoad: function (options) {

  },

  onShow: function () {
    this.checkLoginStatus();
  },

  checkLoginStatus() {
    const token = wx.getStorageSync('token');
    const storedUserInfo = wx.getStorageSync('userInfo'); // 假设登录时存储了userInfo
    if (token && storedUserInfo) {
      this.setData({
        isLoggedIn: true,
        userInfo: {
          username: storedUserInfo.username,
          roles: storedUserInfo.roles || []
        }
      });
      // 如果是从结算页来，并且已登录，则打开地址弹窗
      if (this.data.fromCheckout && !this.data.showAddressModal) {
          this.openAddressModal(true);
      }
    } else {
      this.setData({
        isLoggedIn: false,
        userInfo: { username: '未登录', roles: [] },
        addresses: [] // 未登录则清空地址
      });
    }
  },

  navigateToLogin() {
    wx.navigateTo({ url: '/pages/login/login' });
  },

  navigateToOrders() {
    if (!this.data.isLoggedIn) {
      this.navigateToLogin();
      return;
    }
    wx.navigateTo({ url: '/pages/orders/orders' }); // 确保路径正确
  },

  // --- 地址管理相关逻辑 ---
  openAddressModal(isForSelection = false) {
    if (!this.data.isLoggedIn) {
      this.navigateToLogin();
      return;
    }
    this.setData({
      showAddressModal: true,
      isEditingAddress: false, // 确保打开时不是编辑或新增状态
      isAddingAddress: false,
      // fromCheckout: isForSelection, // 更新是否为选择模式的状态，已在onLoad处理
    });
    this.loadUserAddresses();
  },

  closeAddressModal() {
    this.setData({
      showAddressModal: false,
      isEditingAddress: false,
      isAddingAddress: false,
      // fromCheckout: false // 关闭弹窗时重置
    });
    // 如果是从结算页来，并且没有选择地址就关闭了弹窗，需要通知结算页
    if (this.data.fromCheckout && !this.data.selectedAddressForCheckout) {
        const eventChannel = this.getOpenerEventChannel();
        if (eventChannel) {
            eventChannel.emit('addressSelectionCancelled');
        }
    }
  },

  loadUserAddresses() {
    this.setData({ addressLoading: true });
    const token = wx.getStorageSync('token');
    wx.request({
      url: `${API_BASE_URL}/Address/GetMyAddresses`,
      method: 'GET',
      header: { 'Authorization': `Bearer ${token}` },
      success: (res) => {
        if (res.statusCode === 200) {
          this.setData({ addresses: res.data || [] });
        } else {
          wx.showToast({ title: '加载地址失败', icon: 'none' });
          this.setData({ addresses: [] });
        }
      },
      fail: () => {
        wx.showToast({ title: '网络错误', icon: 'none' });
        this.setData({ addresses: [] });
      },
      complete: () => {
        this.setData({ addressLoading: false });
      }
    });
  },

  startAddAddress() {
    this.setData({
      isAddingAddress: true,
      isEditingAddress: false,
      addressFormData: { // 重置表单
        id: null, contactName: '', phoneNumber: '', province: '', city: '',
        streetAddress: '', postalCode: '', isDefault: false
      },
      region: ['请选择', '请选择', '请选择']
    });
  },

  startEditAddress(e) {
    const address = e.currentTarget.dataset.address;
    this.setData({
      isEditingAddress: true,
      isAddingAddress: false,
      addressFormData: { ...address }, // 浅拷贝地址数据到表单
      region: [address.province || '请选择', address.city || '请选择', ''] // 地区选择器通常只有省市，区在详细地址
    });
  },

  cancelAddressForm() {
    this.setData({ isEditingAddress: false, isAddingAddress: false });
    // 如果之前是空的地址列表，取消添加后重新加载以显示空状态提示
    if (this.data.addresses.length === 0 && !this.data.addressLoading) {
        this.loadUserAddresses(); // 刷新列表视图
    }
  },

  handleAddressFormInput(e) {
    const field = e.currentTarget.dataset.field;
    this.setData({
      [`addressFormData.${field}`]: e.detail.value
    });
  },

  handleAddressFormSwitch(e) {
    const field = e.currentTarget.dataset.field;
    this.setData({
      [`addressFormData.${field}`]: e.detail.value
    });
  },

  onRegionChange: function (e) {
    const regionValue = e.detail.value;
    this.setData({
      region: regionValue,
      'addressFormData.province': regionValue[0],
      'addressFormData.city': regionValue[1]
      // 'addressFormData.district': regionValue[2] // 如果有区县
    });
  },

  saveAddress() {
    if (this.data.savingAddress) return;

    const formData = this.data.addressFormData;
    // 基本前端校验
    if (!formData.contactName.trim()) { wx.showToast({ title: '请输入联系人', icon: 'none' }); return; }
    if (!formData.phoneNumber.trim() || !/^1[3-9]\d{9}$/.test(formData.phoneNumber.trim())) { wx.showToast({ title: '请输入正确的手机号', icon: 'none' }); return; }
    if (!formData.province || formData.province === '请选择') { wx.showToast({ title: '请选择所在地区', icon: 'none' }); return; }
    if (!formData.streetAddress.trim()) { wx.showToast({ title: '请输入详细地址', icon: 'none' }); return; }
    if (formData.postalCode && !/^\d{6}$/.test(formData.postalCode.trim())) { wx.showToast({ title: '请输入6位邮编', icon: 'none' }); return; }


    this.setData({ savingAddress: true });
    const token = wx.getStorageSync('token');
    const url = this.data.isEditingAddress ? `${API_BASE_URL}/Address/UpdateAddress/${formData.id}` : `${API_BASE_URL}/Address/CreateAddress`;
    const method = this.data.isEditingAddress ? 'PUT' : 'POST';

    const requestData = {
      contactName: formData.contactName.trim(),
      phoneNumber: formData.phoneNumber.trim(),
      province: formData.province,
      city: formData.city,
      streetAddress: formData.streetAddress.trim(),
      postalCode: formData.postalCode ? formData.postalCode.trim() : null,
      isDefault: formData.isDefault
    };

    wx.request({
      url: url,
      method: method,
      header: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' },
      data: requestData,
      success: (res) => {
        if (res.statusCode === 200 || res.statusCode === 201 || res.statusCode === 204) {
          wx.showToast({ title: '保存成功', icon: 'success' });
          this.setData({ isEditingAddress: false, isAddingAddress: false });
          this.loadUserAddresses(); // 重新加载地址列表
          // 如果是为结算页选择地址且是新增/编辑后直接选中
          if (this.data.fromCheckout && res.data && (res.statusCode === 201 || res.statusCode === 200)) { // 201是CreateAddress返回，200是Update可能返回（如果返回了对象）
              const savedAddress = this.data.isAddingAddress ? res.data : { ...this.data.addressFormData, id: this.data.addressFormData.id }; // 更新时id不变
              this.selectAndReturnAddress(savedAddress);
          }

        } else {
          let errorMsg = '保存失败';
          if (res.data && res.data.errors) {
            const errors = res.data.errors;
            const firstErrorKey = Object.keys(errors)[0];
            if (firstErrorKey && errors[firstErrorKey] && errors[firstErrorKey].length > 0) {
              errorMsg = errors[firstErrorKey][0];
            }
          } else if (res.data && res.data.message) {
             errorMsg = res.data.message;
          }
          wx.showToast({ title: errorMsg, icon: 'none', duration: 2500 });
        }
      },
      fail: () => {
        wx.showToast({ title: '网络请求失败', icon: 'none' });
      },
      complete: () => {
        this.setData({ savingAddress: false });
      }
    });
  },

  confirmDeleteAddressModal(e) {
    const id = e.currentTarget.dataset.id;
    const name = e.currentTarget.dataset.name;
    this.setData({
      showDeleteAddressConfirmModal: true,
      addressToDelete: { id: id, name: name }
    });
  },
  cancelDeleteAddressModal() {
    this.setData({ showDeleteAddressConfirmModal: false, addressToDelete: { id: null, name: '' } });
  },
  executeDeleteAddress() {
    if (this.data.deletingAddress) return;
    this.setData({ deletingAddress: true });
    const token = wx.getStorageSync('token');
    const addressId = this.data.addressToDelete.id;

    wx.request({
      url: `${API_BASE_URL}/Address/DeleteAddress/${addressId}`,
      method: 'DELETE',
      header: { 'Authorization': `Bearer ${token}` },
      success: (res) => {
        if (res.statusCode === 200 || res.statusCode === 204) {
          wx.showToast({ title: '删除成功', icon: 'success' });
          this.loadUserAddresses(); // 重新加载
        } else {
          wx.showToast({ title: `删除失败: ${res.data.message || res.statusCode}`, icon: 'none' });
        }
      },
      fail: () => { wx.showToast({ title: '网络错误', icon: 'none' }); },
      complete: () => {
        this.setData({ showDeleteAddressConfirmModal: false, addressToDelete: { id: null, name: '' }, deletingAddress: false });
      }
    });
  },

  // 地址列表项点击事件（主要用于结算页选择地址）
  handleAddressItemTap(e) {
    if (this.data.isEditingAddress || this.data.isAddingAddress) return; // 表单状态下不响应列表点击

    if (this.data.fromCheckout) {
      const selectedAddress = e.currentTarget.dataset.address;
      this.selectAndReturnAddress(selectedAddress);
    } else {
      // 非结算页选择模式下，可以考虑设为默认地址或不操作
      // 例如： this.setDefaultAddress(e.currentTarget.dataset.address.id);
    }
  },

  selectAndReturnAddress(address) {
    this.setData({ selectedAddressForCheckout: address });
    const eventChannel = this.getOpenerEventChannel();
    if (eventChannel && eventChannel.emit) {
      eventChannel.emit('acceptSelectedAddress', { address: address });
      wx.navigateBack();
    } else {
      // 兼容方案
      wx.setStorageSync('selectedAddressForCheckout', address);
      wx.navigateBack();
    }
    this.setData({ showAddressModal: false }); // 关闭弹窗
  },


  setDefaultAddress(addressId) { // 示例：设为默认地址的逻辑，可以绑定到地址项的某个按钮
    if (!addressId) return;
    const token = wx.getStorageSync('token');
    wx.showLoading({ title: '设置中...' });
    wx.request({
      url: `${API_BASE_URL}/Address/SetDefaultAddress/${addressId}`,
      method: 'PUT',
      header: { 'Authorization': `Bearer ${token}` },
      success: (res) => {
        if (res.statusCode === 200 || res.statusCode === 204) {
          wx.showToast({ title: '设置成功', icon: 'success' });
          this.loadUserAddresses(); // 重新加载以更新默认状态
        } else {
          wx.showToast({ title: `设置失败: ${res.data.message || '请重试'}`, icon: 'none' });
        }
      },
      fail: () => wx.showToast({ title: '网络错误', icon: 'none' }),
      complete: () => wx.hideLoading()
    });
  },


  // --- 登出逻辑 ---
  logout() {
    wx.showModal({
      title: '提示',
      content: '确定要退出登录吗？',
      success: (res) => {
        if (res.confirm) {
          wx.removeStorageSync('token');
          wx.removeStorageSync('userInfo'); // 清除所有登录相关缓存
          this.setData({
            isLoggedIn: false,
            userInfo: { username: '未登录', roles: [] },
            addresses: []
          });
          wx.showToast({ title: '已退出登录', icon: 'none' });
          wx.reLaunch({ url: '/pages/login/login' });
        }
      }
    });
  },

  preventTouchMove() {
    return; // 阻止弹窗背景滚动
  }
});