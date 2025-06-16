// .prettierrc.js

module.exports = {
  // 不在语句末尾添加分号 (例如：const x = 1 而不是 const x = 1;)
  semi: false,
  // 使用单引号而不是双引号 (例如：'hello' 而不是 "hello")
  // 根据您报错信息 "Replace '...' with "..."，这表明您的Prettier当前配置是使用双引号。
  // 如果您想修复这个错误并继续使用双引号，请将此设置为 `false`。
  // 如果您更喜欢单引号，可以设置为 `true`，但需要再次运行修复命令来统一。
  // 我这里选择 `false` 以匹配您报错信息中 Prettier 期望的格式。
  singleQuote: false,
  // 超过100字符时换行
  printWidth: 100,
  // 缩进2个空格
  tabWidth: 2,
  // 在ES5中有效的对象和数组后面添加逗号 (例如：{ a: 1, b: 2, })
  trailingComma: "es5",
  // 强制使用LF作为行结束符 (Unix-style)
  // 这是解决 "Delete ␍" 错误的关键。
  endOfLine: "lf",
  // 对象属性之间打印空格 (例如：{ foo: bar } 而不是 {foo:bar})
  bracketSpacing: true,
  // 箭头函数参数只有一个时，也加上括号 (例如：(x) => x 而不是 x => x)
  arrowParens: "always",
}
