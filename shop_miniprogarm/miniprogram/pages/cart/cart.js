const API_BASE_URL = 'https://localhost:7024/api'; // 您的API基础路径

Page({
  data: {
    loading: true,
    cart: {
      cartId: 0,
      items: [], // CartItemViewModel[]
      totalAmount: 0,
      totalItemsCount: 0,
      lastModifiedDate: ''
    },
    selectAll: false,
    selectedTotalAmount: 0,
    selectedItemsCount: 0,
  },

  onShow() { // 使用onShow，每次进入页面都刷新购物车
    this.getCartData();
  },

  getCartData() {
    this.setData({ loading: true });
    const token = wx.getStorageSync('token');
    if (!token) {
      wx.showModal({
        title: '提示',
        content: '您尚未登录，请先登录',
        confirmText: '去登录',
        cancelText: '暂不登录',
        success: (res) => {
          if (res.confirm) {
            wx.navigateTo({ url: '/pages/login/login' }); // 根据您的登录页路径调整
          } else {
            this.setData({ loading: false, cart: { items: [] } }); // 未登录则显示空购物车
          }
        }
      });
      return;
    }

    wx.request({
      url: `${API_BASE_URL}/Cart/GetMyCart`,
      method: 'GET',
      header: { 'Authorization': `Bearer ${token}` },
      success: (res) => {
        if (res.statusCode === 200) {
          const cartData = res.data;
          // 给每个商品项增加一个 selected 状态，默认为 true
          if (cartData.items) {
            cartData.items.forEach(item => {
              item.selected = true; // 默认全部选中
              // 检查库存，如果数量超过库存，则修正数量（理论上后端GetMyCart已处理，这里做双重保险）
              if (item.quantity > item.stock && item.stock > 0) {
                  item.quantity = item.stock;
                  // 可以考虑提示用户数量已调整
              } else if (item.stock <= 0) {
                  item.quantity = 0; // 无库存时数量设为0，但不直接移除，让用户看到并手动删除或后端处理
              }
            });
          }
          this.setData({
            cart: cartData,
            loading: false,
          });
          this.updateSelectionAndSummary(); // 更新选中状态和总计
        } else {
          wx.showToast({ title: `获取购物车失败: ${res.data.message || res.statusCode}`, icon: 'none' });
          this.setData({ loading: false, cart: { items: [] } });
        }
      },
      fail: (err) => {
        wx.showToast({ title: '网络请求失败', icon: 'none' });
        console.error("GetMyCart fail", err);
        this.setData({ loading: false, cart: { items: [] } });
      }
    });
  },

  // 更新选中状态和汇总信息
  updateSelectionAndSummary() {
    let selectedTotalAmount = 0;
    let selectedItemsCount = 0;
    let allSelected = true;
    const items = this.data.cart.items || [];

    if (items.length === 0) {
      allSelected = false;
    } else {
      items.forEach(item => {
        if (item.selected && item.stock > 0 && item.quantity > 0) { // 只有选中的、有库存的、有数量的才计入结算
          selectedTotalAmount += item.price * item.quantity;
          selectedItemsCount += item.quantity; // 或者 item.selected ? 1 : 0; 如果按种类算
        }
        if (!item.selected) {
          allSelected = false;
        }
      });
    }
    this.setData({
      selectAll: allSelected,
      selectedTotalAmount: selectedTotalAmount,
      selectedItemsCount: selectedItemsCount
    });
  },

  // 单个商品选中/取消选中
  toggleSelect(e) {
    const itemId = e.currentTarget.dataset.itemid;
    const items = this.data.cart.items.map(item => {
      if (item.cartItemId === itemId) {
        item.selected = !item.selected;
      }
      return item;
    });
    this.setData({ 'cart.items': items });
    this.updateSelectionAndSummary();
  },

  // 全选/取消全选
  toggleSelectAll() {
    const newSelectAll = !this.data.selectAll;
    const items = this.data.cart.items.map(item => {
      item.selected = newSelectAll;
      return item;
    });
    this.setData({
      'cart.items': items,
      selectAll: newSelectAll
    });
    this.updateSelectionAndSummary();
  },

  // 修改数量（加减按钮）
  changeQuantity(e) {
    const { itemid, type, current, stock } = e.currentTarget.dataset;
    let newQuantity = parseInt(current);

    if (type === 'plus') {
      if (newQuantity < stock) {
        newQuantity++;
      } else {
        wx.showToast({ title: '已达到库存上限', icon: 'none' });
        return;
      }
    } else if (type === 'minus') {
      if (newQuantity > 1) {
        newQuantity--;
      } else {
        // 数量为1时再减可以考虑提示用户是否删除，或者不做任何操作
        // wx.showToast({ title: '数量不能小于1', icon: 'none' });
        return; // 不允许减到0
      }
    }
    this.updateItemQuantityInCart(itemid, newQuantity);
  },

  // 输入框失焦时更新数量
  inputQuantity(e) {
    const itemId = e.currentTarget.dataset.itemid;
    const stock = parseInt(e.currentTarget.dataset.stock);
    let newQuantity = parseInt(e.detail.value);

    if (isNaN(newQuantity) || newQuantity < 1) {
      newQuantity = 1; // 非法输入或小于1，则重置为1
      wx.showToast({ title: '数量至少为1', icon: 'none' });
    } else if (newQuantity > stock) {
      newQuantity = stock; // 超过库存，则设为最大库存
      wx.showToast({ title: '已超过库存上限', icon: 'none' });
    }
    // 更新视图上的值，避免输入非法值后输入框不刷新
    const items = this.data.cart.items.map(item => {
        if (item.cartItemId === itemId) {
            item.quantity = newQuantity; // 先更新视图
        }
        return item;
    });
    this.setData({'cart.items': items});
    this.updateSelectionAndSummary(); // 先更新汇总，再请求API

    this.updateItemQuantityInCart(itemId, newQuantity);
  },

  // 调用API更新购物车商品数量
  updateItemQuantityInCart(cartItemId, quantity) {
    this.setData({loading: true});
    const token = wx.getStorageSync('token');
    wx.request({
      url: `${API_BASE_URL}/Cart/UpdateItemQuantity`,
      method: 'PUT',
      header: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' },
      data: {
        cartItemId: cartItemId,
        quantity: quantity
      },
      success: (res) => {
        if (res.statusCode === 200) {
          // 后端成功后会返回更新后的购物车，可以重新赋值或部分更新
          // 为简化，我们信任后端数据，并再次获取或直接使用返回的购物车
          // this.getCartData(); // 重新获取整个购物车，保证数据一致性
          // 或者，如果后端返回了更新后的购物车：
          const updatedCart = res.data;
           if (updatedCart.items) {
            updatedCart.items.forEach(item => {
                // 找到当前页面数据中对应的项，并恢复其选中状态，因为后端返回的cartItem没有selected
                const currentItem = this.data.cart.items.find(i => i.cartItemId === item.cartItemId);
                item.selected = currentItem ? currentItem.selected : true;
                 if (item.quantity > item.stock && item.stock > 0) item.quantity = item.stock;
                 else if (item.stock <= 0) item.quantity = 0;
            });
          }
          this.setData({ cart: updatedCart });
          this.updateSelectionAndSummary();

        } else {
          wx.showToast({ title: `更新数量失败: ${res.data.message || res.statusCode}`, icon: 'none' });
          this.getCartData(); // 操作失败时，重新从服务器拉取数据以同步状态
        }
      },
      fail: () => {
        wx.showToast({ title: '网络请求失败', icon: 'none' });
        this.getCartData();
      },
      complete: () => {
        this.setData({loading: false});
      }
    });
  },

  // 删除购物车商品
  removeItem(e) {
    const itemId = e.currentTarget.dataset.itemid;
    const productName = e.currentTarget.dataset.name;
    wx.showModal({
      title: '确认删除',
      content: `确定要从购物车中删除 "${productName}" 吗？`,
      success: (res) => {
        if (res.confirm) {
          this.setData({loading: true});
          const token = wx.getStorageSync('token');
          wx.request({
            url: `${API_BASE_URL}/Cart/RemoveItemFromCart/${itemId}`,
            method: 'DELETE',
            header: { 'Authorization': `Bearer ${token}` },
            success: (delRes) => {
              if (delRes.statusCode === 200) {
                wx.showToast({ title: '删除成功', icon: 'success' });
                // this.getCartData(); // 重新获取购物车
                // 或者，如果后端返回了更新后的购物车：
                const updatedCart = delRes.data;
                 if (updatedCart.items) {
                  updatedCart.items.forEach(item => {
                      item.selected = true; // 假设删除后，剩余的还是选中状态
                  });
                }
                this.setData({ cart: updatedCart });
                this.updateSelectionAndSummary();

              } else {
                wx.showToast({ title: `删除失败: ${delRes.data.message || delRes.statusCode}`, icon: 'none' });
              }
            },
            fail: () => {
              wx.showToast({ title: '网络请求失败', icon: 'none' });
            },
            complete: () => {
              this.setData({loading: false});
            }
          });
        }
      }
    });
  },

  // 去结算
  goToCheckout() {
    if (this.data.selectedItemsCount === 0) {
      wx.showToast({ title: '请至少选择一件商品', icon: 'none' });
      return;
    }
    // 收集选中的商品信息传递给结算页
    const selectedItems = this.data.cart.items.filter(item => item.selected && item.stock > 0 && item.quantity > 0);
    if (selectedItems.length === 0) {
         wx.showToast({ title: '所选商品均无库存或数量为0', icon: 'none' });
         return;
    }
    // 将选中的商品数据存储到全局变量或通过页面跳转参数传递
    // 推荐使用全局状态管理（如MobX, Vuex类似库）或 wx.setStorageSync 临时存储
    wx.setStorageSync('checkoutItems', selectedItems);
    wx.navigateTo({ url: '/pages/checkout/checkout' }); // 确保结算页路径正确
  },

  // 去逛逛（空购物车时）
  goShopping() {
    wx.switchTab({ url: '/pages/products/products' }); // 假设您的商品列表页是这个路径并且是Tab页
    // 如果不是Tab页，使用 wx.navigateTo
  },

  // 跳转到商品详情 (可选)
  gotoProductDetail(e) {
    const productId = e.currentTarget.dataset.productid;
    wx.navigateTo({ url: `/pages/productDetail/productDetail?id=${productId}` }); // 假设商品详情页路径
  }
});