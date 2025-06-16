// shop_miniprogarm/miniprogram/pages/products/products.js

const API_BASE_URL = 'https://localhost:7024/api'; // 请确保这里的API_BASE_URL与你的后端配置一致

Page({
  data: {
    products: [],
    currentPage: 1,
    pageSize: 9,
    totalPages: 1,
    hasMoreData: true,
    loading: true,
    isLoadingMore: false,
    networkError: false,
    scrollViewHeight: 0,
    showBackToTop: false,
    scrollTop: 0,

    // 新增：商品详情弹窗相关数据
    showProductDetailModal: false,
    currentProductDetail: null,
    productDetailLoading: false
  },

  onLoad: function (options) {
    this.setScrollViewHeight();
    this.loadProducts(true);
  },

  setScrollViewHeight() {
    const systemInfo = wx.getSystemInfoSync();
    const windowHeight = systemInfo.windowHeight;
    let searchBarHeight = 0; // 如果有搜索栏，需要获取其高度并减去
    this.setData({
      scrollViewHeight: windowHeight - searchBarHeight
    });
  },

  loadProducts(isRefresh = false) {
    if (isRefresh) {
      this.setData({
        products: [],
        currentPage: 1,
        hasMoreData: true,
        loading: true,
        networkError: false
      });
    }
    if (!this.data.hasMoreData || this.data.isLoadingMore) {
      if (!isRefresh) this.setData({ loading: false });
      return;
    }
    this.setData({ isLoadingMore: true, loading: isRefresh });
    wx.request({
      url: `${API_BASE_URL}/Product/List`,
      method: 'GET',
      data: {
        pageIndex: this.data.currentPage,
        pageSize: this.data.pageSize,
      },
      success: (res) => {
        if (res.statusCode === 200) {
          const backendData = res.data;
          const newProducts = backendData.items || [];
          newProducts.forEach(p => p.addingToCart = false);
          this.setData({
            products: isRefresh ? newProducts : this.data.products.concat(newProducts),
            totalPages: backendData.totalPages || 1,
            hasMoreData: this.data.currentPage < (backendData.totalPages || 1),
            currentPage: this.data.currentPage + (newProducts.length > 0 ? 1 : 0),
            networkError: false
          });
        } else {
          wx.showToast({ title: `加载商品失败: ${res.statusCode}`, icon: 'none' });
          this.setData({ networkError: true });
        }
      },
      fail: (err) => {
        wx.showToast({ title: '网络请求失败', icon: 'none' });
        console.error("loadProducts fail", err);
        this.setData({ networkError: true });
      },
      complete: () => {
        this.setData({ loading: false, isLoadingMore: false });
      }
    });
  },

  loadMoreProducts() {
    if (this.data.hasMoreData && !this.data.isLoadingMore) {
      this.loadProducts();
    }
  },

  onPullDownRefresh() {
    this.loadProducts(true);
    wx.stopPullDownRefresh();
  },

  // 修改 navigateToDetail 函数以显示商品详情弹窗
  navigateToDetail(e) {
    const productId = e.currentTarget.dataset.id;
    if (!productId) return;

    this.setData({
      showProductDetailModal: true,
      productDetailLoading: true,
      currentProductDetail: null // 清空旧数据
    });

    wx.request({
      url: `${API_BASE_URL}/Product/${productId}`,
      method: 'GET',
      success: (res) => {
        if (res.statusCode === 200) {
          this.setData({
            currentProductDetail: res.data,
            productDetailLoading: false
          });
        } else {
          wx.showToast({ title: `获取商品详情失败: ${res.data.message || res.statusCode}`, icon: 'none' });
          this.setData({
            showProductDetailModal: false, // 关闭弹窗
            productDetailLoading: false
          });
        }
      },
      fail: (err) => {
        wx.showToast({ title: '网络请求失败', icon: 'none' });
        console.error("getProductDetail fail", err);
        this.setData({
          showProductDetailModal: false, // 关闭弹窗
          productDetailLoading: false
        });
      }
    });
  },

  // 新增：关闭商品详情弹窗
  closeProductDetailModal() {
    this.setData({
      showProductDetailModal: false,
      currentProductDetail: null,
      productDetailLoading: false
    });
  },

  // 新增：防止弹窗滚动穿透
  preventTouchMove() {
    return;
  },

  // 修改 addToCart 函数，使其能从商品详情弹窗中调用
  addToCart(e) {
    // 从事件中获取 product 数据，如果从弹窗调用，则直接使用 currentProductDetail
    const product = e.currentTarget.dataset.product || this.data.currentProductDetail;

    if (!product || product.addingToCart) return;

    const token = wx.getStorageSync('token');
    if (!token) {
      wx.showModal({
        title: '提示',
        content: '请先登录再操作',
        confirmText: '去登录',
        success: res => {
          if (res.confirm) wx.navigateTo({ url: '/pages/login/login' });
        }
      });
      return;
    }

    this.setProductAddingState(product.id, true);
    wx.request({
      url: `${API_BASE_URL}/Cart/AddItemToCart`,
      method: 'POST',
      header: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' },
      data: {
        productId: product.id,
        quantity: 1 // 默认添加数量为1
      },
      success: (res) => {
        if (res.statusCode === 200 || res.statusCode === 201) {
          wx.showToast({ title: '添加成功', icon: 'success', duration: 1000 });
        } else {
          let errorMsg = '添加失败';
          if (res.data && res.data.message) errorMsg = res.data.message;
          else if (res.data && res.data.title) errorMsg = res.data.title;
          wx.showToast({ title: errorMsg, icon: 'none' });
        }
      },
      fail: () => {
        wx.showToast({ title: '网络请求失败', icon: 'none' });
      },
      complete: () => {
        this.setProductAddingState(product.id, false);
      }
    });
  },

  setProductAddingState(productId, isAdding) {
    const products = this.data.products.map(p => {
      if (p.id === productId) {
        p.addingToCart = isAdding;
      }
      return p;
    });
    this.setData({ products });

    // 如果当前弹窗显示的商品就是正在操作的商品，也更新其状态
    if (this.data.currentProductDetail && this.data.currentProductDetail.id === productId) {
        this.setData({
            'currentProductDetail.addingToCart': isAdding
        });
    }
  },

  onPageScroll(e) {
    const scrollTop = e.scrollTop;
    this.setData({
      showBackToTop: scrollTop > 300,
      scrollTop: scrollTop
    });
  },

  backToTop() {
    wx.pageScrollTo({
      scrollTop: 0,
      duration: 300
    });
  },

  onSearch(e) {
    const keyword = e.detail.value || "";
    wx.showToast({ title: `搜索: ${keyword}`, icon: 'none' });
    // TODO: 实现搜索逻辑，重新加载商品列表
  }
});