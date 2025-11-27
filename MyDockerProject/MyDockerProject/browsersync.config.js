module.exports = {
  proxy: "http://localhost:44372", // or 44372, depending on your final port
  files: ["Views/**/*.cshtml", "wwwroot/css/**/*.css", "wwwroot/js/**/*.js"],
  notify: false,
  open: true,
  reloadDelay: 250,
};
