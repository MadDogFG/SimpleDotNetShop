const API_BASE_URL = 'https://localhost:7024/api';

Page({
  data: {
    checkoutItems: [],    // 从购物车传来的商品 CartItemViewModel[]
    selectedAddress: null,// AddressViewModel
    orderNotes: '',
    productTotalAmount: 0,
    shippingFee: 0,       // 运费，默认为0，可以根据地址或商品计算
    discountAmount: 0,    // 优惠金额
    finalTotalAmount: 0,
    submittingOrder: false,
    pageLoading: true,
  },

  onLoad: function (options) {
    const items = wx.getStorageSync('checkoutItems');
    if (items && items.length > 0) {
      this.setData({ checkoutItems: items });
      this.calculateTotals();
      this.loadDefaultAddress(); // 加载默认或上次选择的地址
    } else {
      // 没有商品，不应该能进入结算页，引导回购物车或首页
      wx.showModal({
        title: '提示',
        content: '没有需要结算的商品，请先去购物车选择。',
        showCancel: false,
        confirmText: '去购物车',
        success: () => {
          wx.navigateBack({ delta: 1 }); // 返回上一页，通常是购物车
          // 或者 wx.switchTab({ url: '/pages/cart/cart' });
        }
      });
    }
  },

  onShow: function() {
    // 监听从地址选择页面返回的事件 (通过 eventChannel)
    // 或者检查 Storage 中是否有新选择的地址 (兼容方案)
    const selectedAddrFromStorage = wx.getStorageSync('selectedAddressForCheckout');
    if (selectedAddrFromStorage) {
        this.setData({ selectedAddress: selectedAddrFromStorage });
        wx.removeStorageSync('selectedAddressForCheckout'); // 用完即删
        this.calculateTotals(); // 地址可能影响运费
    }
  },

  loadDefaultAddress() {
    this.setData({ pageLoading: true });
    const token = wx.getStorageSync('token');
    if (!token) {
      // 理论上到结算页应该已登录，未登录则提示并返回
      wx.showModal({
        title: '登录提示',
        content: '请先登录再进行结算',
        confirmText: '去登录',
        showCancel: false,
        success: res => {
          if (res.confirm) wx.navigateTo({ url: '/pages/login/login' });
          else wx.navigateBack();
        }
      });
      this.setData({ pageLoading: false });
      return;
    }

    wx.request({
      url: `${API_BASE_URL}/Address/GetMyAddresses`, // 获取所有地址
      method: 'GET',
      header: { 'Authorization': `Bearer ${token}` },
      success: (res) => {
        if (res.statusCode === 200 && res.data && res.data.length > 0) {
          const addresses = res.data;
          let defaultAddress = addresses.find(addr => addr.isDefault);
          if (!defaultAddress) {
            defaultAddress = addresses[0]; // 如果没有默认，选第一个
          }
          this.setData({ selectedAddress: defaultAddress });
        } else if (res.statusCode === 200 && (!res.data || res.data.length === 0)) {
          // 没有地址，selectedAddress 保持 null，WXML会提示选择
          this.setData({ selectedAddress: null });
        } else {
          wx.showToast({ title: '加载地址失败', icon: 'none' });
        }
      },
      fail: () => {
        wx.showToast({ title: '网络错误', icon: 'none' });
      },
      complete: () => {
        this.setData({ pageLoading: false });
        this.calculateTotals();
      }
    });
  },

  calculateTotals() {
    let productTotal = 0;
    this.data.checkoutItems.forEach(item => {
      productTotal += item.price * item.quantity;
    });

    // 简单示例：运费计算 (可以根据地址、重量等复杂化)
    let shippingFee = 0;
    // if (this.data.selectedAddress && productTotal > 0 && productTotal < 99) { // 例如满99包邮
    //   shippingFee = 10; // 假设运费10元
    // }

    let finalTotal = productTotal + shippingFee - this.data.discountAmount;
    console.log(finalTotal);

    this.setData({
      productTotalAmount: productTotal,
      shippingFee: shippingFee,
      finalTotalAmount: finalTotal < 0 ? 0 : finalTotal // 确保总额不为负
    });
  },

  selectAddress() {
    console.log("跳转到地址管理");
    wx.switchTab({
      url: '/pages/profile/profile',
      events: {
        // 监听被打开页面通过eventChannel传送到当前页面的数据
        acceptSelectedAddress: (data) => {
          console.log('Address selected from profile modal:', data);
          if (data && data.address) {
            this.setData({ selectedAddress: data.address });
            this.calculateTotals(); // 地址变化可能影响运费
          }
        },
        addressSelectionCancelled: () => {
            console.log('Address selection was cancelled from profile modal');
            // 用户在地址弹窗中没有选择地址就关闭了，可以不做特殊处理或给出提示
        }
      },
      success: (res) => {
        // 可选：向被打开页面传送数据
        // res.eventChannel.emit('acceptDataFromOpenerPage', { data: 'some data' });
      }
    });
  },

  onNotesInput(e) {
    this.setData({ orderNotes: e.detail.value });
  },

  submitOrder() {
    if (!this.data.selectedAddress || !this.data.selectedAddress.id) {
      wx.showToast({ title: '请选择收货地址', icon: 'none' });
      return;
    }
    if (!this.data.checkoutItems || this.data.checkoutItems.length === 0) {
      wx.showToast({ title: '没有需要结算的商品', icon: 'none' });
      return;
    }
    if (this.data.submittingOrder) return;

    this.setData({ submittingOrder: true });
    const token = wx.getStorageSync('token');

    const orderItemsDto = this.data.checkoutItems.map(item => ({
      productId: item.productId,
      quantity: item.quantity
    }));

    const requestData = {
      items: orderItemsDto,
      shippingAddressId: this.data.selectedAddress.id,
      notes: this.data.orderNotes
    };

    wx.request({
      url: `${API_BASE_URL}/Order/CreateOrder`,
      method: 'POST',
      header: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' },
      data: requestData,
      success: (res) => {
        if (res.statusCode === 201 && res.data) { // 201 Created
          const createdOrder = res.data; // OrderViewModel
          wx.showToast({ title: '订单提交成功！', icon: 'success' });
          
          // 清理购物车中已下单的商品 (重要)
          this.clearOrderedItemsFromCart(this.data.checkoutItems);

          // 跳转到订单详情页或订单列表页
          wx.redirectTo({ // 使用redirectTo关闭当前页，避免返回
            url: `/pages/orders/orders` // 跳转到新创建的订单详情
          });
        } else {
          let errorMsg = '订单提交失败';
            if (res.data) {
                if (res.data.errors) { // FluentValidation 错误
                    const errors = res.data.errors;
                    const firstErrorField = Object.keys(errors)[0];
                    if (errors[firstErrorField] && errors[firstErrorField].length > 0) {
                        errorMsg = errors[firstErrorField][0];
                    }
                } else if (res.data.message) { // 自定义错误消息
                    errorMsg = res.data.message;
                } else if (res.data.title) { // ASP.NET Core ProblemDetails
                    errorMsg = res.data.title;
                } else if (typeof res.data === 'string') {
                    errorMsg = res.data;
                }
            }
          wx.showModal({ title: '下单失败', content: errorMsg || '请稍后重试', showCancel:false });
        }
      },
      fail: (err) => {
        wx.showToast({ title: '网络请求失败，请重试', icon: 'none' });
        console.error("SubmitOrder fail", err);
      },
      complete: () => {
        this.setData({ submittingOrder: false });
      }
    });
  },

  // 清理购物车中已下单的商品
  async clearOrderedItemsFromCart(orderedItems) {
    const token = wx.getStorageSync('token');
    if (!token || !orderedItems || orderedItems.length === 0) return;

    console.log("订单创建成功，以下商品理论上应从购物车移除（或标记）：",
      orderedItems.map(i => `ProductId: ${i.productId}, CartItemId: ${i.cartItemId}`));

    // 从 orderedItems (即 checkoutItems，它们是 CartItemViewModel) 中提取 cartItemId
    const cartItemIdsToRemove = orderedItems.map(item => item.cartItemId).filter(id => id != null);

    if (cartItemIdsToRemove.length > 0) {
      wx.request({
        url: `${API_BASE_URL}/Cart/RemoveMultipleItemsFromCart`,
        method: 'POST', // 使用 POST 方法
        header: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        },
        data: {
          cartItemIds: cartItemIdsToRemove // 发送 CartItemId 列表
        },
        success: (res) => {
          if (res.statusCode === 200) {
            console.log('成功批量移除已下单商品:', res.data);
            getApp().globalData.cartNeedRefresh = true; // 标记购物车需要刷新
          } else {
            console.error('批量移除购物车商品失败:', res.data);
            wx.showToast({ title: `移除失败: ${res.data.message || res.statusCode}`, icon: 'none' });
          }
        },
        fail: (err) => {
          console.error('批量移除购物车商品网络请求失败:', err);
          wx.showToast({ title: '网络错误，批量移除失败', icon: 'none' });
        }
      });
    } else {
      console.log("没有需要从购物车中移除的商品项。");
    }

    // 即使没有具体商品要移除，也可能需要刷新购物车状态（例如，清空购物车后）
    getApp().globalData.cartNeedRefresh = true;
  },
});