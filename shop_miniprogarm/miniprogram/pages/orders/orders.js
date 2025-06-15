const API_BASE_URL = 'https://localhost:7024/api';
const util = require('../../utils/util.js'); // 假设路径是 ../../utils/util.js

Page({
  data: {
    orders: [],
    currentPage: 1,
    pageSize: 10,
    totalPages: 1,
    hasMoreData: true,
    loading: true,
    isLoadingMore: false,
    networkError: false, // 列表网络错误
    scrollViewHeight: 0,
    highlightedOrderId: null,
    
    tabList: [ // 订单状态筛选Tab
      { name: '全部', statusValue: '' },
      { name: '待发货', statusValue: 'Paid' },
      { name: '待收货', statusValue: 'Shipped' },
      { name: '已完成', statusValue: 'Completed' },
      { name: '已取消', statusValue: 'Cancelled' }
    ],
    currentFilterStatus: '', // 当前筛选的状态值，空字符串表示全部

    showOrderDetailModal: false,
    currentOrderDetail: null,
    orderDetailLoading: false,
    networkErrorOrderDetail: false, // 详情加载网络错误
  },

  onLoad: function (options) {
    this.setScrollViewHeight();
    if (options.highlightOrder) {
      this.setData({ highlightedOrderId: parseInt(options.highlightOrder) });
    }
    if (options.status && this.data.tabList.some(tab => tab.statusValue === options.status)) {
        this.setData({ currentFilterStatus: options.status });
    }
    // this.loadUserOrders(true); // 由onShow统一调用
  },

  onShow: function() {
    this.loadUserOrders(true);
  },

  setScrollViewHeight() {
    const systemInfo = wx.getSystemInfoSync();
    let tabHeight = 0;
    const query = wx.createSelectorQuery().in(this); // 需要 .in(this)
    query.select('.order-tabs').boundingClientRect(rect => {
      if (rect) {
        tabHeight = rect.height;
      }
      this.setData({
        scrollViewHeight: systemInfo.windowHeight - tabHeight
      });
    }).exec();
  },

  onTabClick(e) {
    const status = e.currentTarget.dataset.status;
    if (this.data.currentFilterStatus === status) return;
    this.setData({
      currentFilterStatus: status,
      highlightedOrderId: null, // 切换tab时清除高亮
    });
    this.loadUserOrders(true); // 刷新列表
  },

  loadUserOrders(isRefresh = false) {
    const token = wx.getStorageSync('token');
    if (!token) {
      wx.showModal({ title: '提示', content: '请先登录查看订单', confirmText: '去登录', cancelText: '暂不',
        success: res => { if (res.confirm) wx.navigateTo({url: '/pages/login/login'}); }
      });
      this.setData({loading: false, orders: [], networkError: false});
      return;
    }

    if (isRefresh) {
      this.setData({ orders: [], currentPage: 1, hasMoreData: true, loading: true, networkError: false, scrollTop:0 });
    }

    if ((!this.data.hasMoreData || this.data.isLoadingMore) && !isRefresh) {
      this.setData({loading:false});
      return;
    }

    this.setData({ isLoadingMore: !isRefresh, loading: isRefresh });

    wx.request({
      url: `${API_BASE_URL}/Order/GetMyOrders`,
      method: 'GET',
      header: { 'Authorization': `Bearer ${token}` },
      data: {
        pageIndex: this.data.currentPage,
        pageSize: this.data.pageSize,
        status: this.data.currentFilterStatus || null // 后端接口如果status为空则查全部
      },
      success: (res) => {
        if (res.statusCode === 200) {
          const backendData = res.data;
          let newOrders = backendData.items || [];
          newOrders.forEach(order => {
            order.orderDateFormatted = util.formatTime(new Date(order.orderDate));
            order.actionLoading = false;
          });
          this.setData({
            orders: isRefresh ? newOrders : this.data.orders.concat(newOrders),
            totalPages: backendData.totalPages || 1,
            hasMoreData: this.data.currentPage < (backendData.totalPages || 1),
            currentPage: this.data.currentPage + (newOrders.length > 0 ? 1 : 0),
            networkError: false
          });
        } else {
          this.setData({ networkError: true });
          wx.showToast({ title: `加载订单失败: ${res.data.message || res.statusCode}`, icon: 'none' });
        }
      },
      fail: (err) => {
        this.setData({ networkError: true });
        wx.showToast({ title: '网络请求失败', icon: 'none' });
        console.error("loadUserOrders fail", err);
      },
      complete: () => {
        this.setData({ loading: false, isLoadingMore: false });
      }
    });
  },

  loadMoreOrders() {
    if (this.data.hasMoreData && !this.data.isLoadingMore) {
      this.loadUserOrders();
    }
  },

  onPullDownRefresh() {
    this.loadUserOrders(true);
    wx.stopPullDownRefresh();
  },

  goShopping() {
    wx.switchTab({ url: '/pages/products/products' });
  },

  // --- 订单详情弹窗逻辑 ---
  openOrderDetailModal(e) {
    const orderId = e.currentTarget.dataset.orderid;
    if (!orderId) return;

    this.setData({ showOrderDetailModal: true, orderDetailLoading: true, currentOrderDetail: null, networkErrorOrderDetail: false });
    const token = wx.getStorageSync('token');

    wx.request({
      url: `${API_BASE_URL}/Order/GetUserOrderById/${orderId}`,
      method: 'GET',
      header: { 'Authorization': `Bearer ${token}` },
      success: (res) => {
        if (res.statusCode === 200) {
          const detail = res.data;
          console.log(detail);
          detail.orderDateFormatted = util.formatTime(new Date(detail.orderDate));
          let productTotal = 0;
          if (detail.orderItems) {
            detail.orderItems.forEach(item => productTotal += item.unitPrice * item.quantity);
          }
          detail.productTotalAmount = productTotal;
          // 假设运费是后端计算好的，或者前端根据规则计算
          detail.shippingFee = detail.shippingFee || (detail.totalAmount - productTotal);
          if(detail.shippingFee < 0) detail.shippingFee = 0;

          detail.actionLoading = false;
          this.setData({ currentOrderDetail: detail });
        } else {
          wx.showToast({ title: '获取订单详情失败', icon: 'none' });
          this.setData({ networkErrorOrderDetail: true });
        }
      },
      fail: () => {
        wx.showToast({ title: '网络错误', icon: 'none' });
        this.setData({ networkErrorOrderDetail: true });
      },
      complete: () => {
        this.setData({ orderDetailLoading: false });
      }
    });
  },

  closeOrderDetailModal() {
    this.setData({ showOrderDetailModal: false, currentOrderDetail: null });
  },

  onDetailOverlayTap(e) { // 点击详情弹窗遮罩层关闭
    if (e.target.id === e.currentTarget.id || (e.target.dataset && e.target.dataset.isOverlay)) {
        this.closeOrderDetailModal();
    }
  },
  preventDetailContentTap() { /* 阻止内容点击冒泡 */ },
  retryLoadOrderDetail(){
      if(this.data.currentOrderDetail && this.data.currentOrderDetail.id){
          // 如果是之前打开的详情，用其ID重试
          const tempOrderId = this.data.currentOrderDetail.id;
          this.setData({showOrderDetailModal: false, currentOrderDetail: null}, () => {
              this.openOrderDetailModal({currentTarget: {dataset: {orderid: tempOrderId}}});
          });
      } else {
          this.setData({networkErrorOrderDetail: false}); // 简单重置状态
      }
  },


  // --- 订单操作逻辑 ---
  setOrderActionLoading(orderId, isLoading, isFromModal) {
    if (isFromModal && this.data.currentOrderDetail && this.data.currentOrderDetail.id === orderId) {
        this.setData({ 'currentOrderDetail.actionLoading': isLoading });
    } else {
        const orders = this.data.orders.map(order => {
            if (order.id === orderId) {
                order.actionLoading = isLoading;
            }
            return order;
        });
        this.setData({ orders });
    }
  },
  handleOrderAction(e) {
    const { action, orderid } = e.currentTarget.dataset;
    this.executeOrderAction(action, orderid, false);
  },
  handleOrderActionInModal(e) {
    const { action, orderid } = e.currentTarget.dataset;
    this.executeOrderAction(action, orderid, true);
  },
  executeOrderAction(action, orderId, isFromModal) {
    const token = wx.getStorageSync('token');
    let url = '';
    let successMsg = '';
    let confirmTitle = '确认操作';
    let confirmContent = '';
    let httpMethod = 'PUT'; // 默认是PUT

    if (action === 'cancel') {
      url = `${API_BASE_URL}/Order/CancelMyOrder/${orderId}`;
      successMsg = '订单已取消';
      confirmContent = '确定要取消这个订单吗？';
    } else if (action === 'confirmReceipt') {
      url = `${API_BASE_URL}/Order/ConfirmReceipt/${orderId}`; // 【后端需要实现此接口】
      successMsg = '确认收货成功';
      confirmContent = '您确定已经收到该订单的商品了吗？';
    } else {
      console.warn("Unknown order action:", action);
      return;
    }

    wx.showModal({
      title: confirmTitle,
      content: confirmContent,
      success: (res) => {
        if (res.confirm) {
          this.setOrderActionLoading(orderId, true, isFromModal);
          wx.request({
            url: url,
            method: httpMethod,
            header: { 'Authorization': `Bearer ${token}` },
            success: (apiRes) => {
              if (apiRes.statusCode === 200 || apiRes.statusCode === 204) {
                wx.showToast({ title: successMsg, icon: 'success' });
                if (isFromModal) {
                  this.closeOrderDetailModal();
                }
                this.loadUserOrders(true); // 刷新订单列表
              } else {
                wx.showToast({ title: `操作失败: ${apiRes.data.message || '请重试'}`, icon: 'none' });
              }
            },
            fail: () => wx.showToast({ title: '网络错误', icon: 'none' }),
            complete: () => this.setOrderActionLoading(orderId, false, isFromModal)
          });
        }
      }
    });
  },

  // --- 页面滚动与回到顶部 ---
  onPageScroll(e) {

  },
  preventTouchMove() { /* 阻止弹窗背景滚动 */ }
});