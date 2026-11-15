mergeInto(LibraryManager.library, {
 LR_ReportSmoke: function(text) {
  var value=UTF8ToString(text);
  window.lunarSmokeResult=value;
  document.documentElement.setAttribute('data-lunar-smoke',value.indexOf('ALL CHECKS PASSED')>=0?'passed':value.indexOf('CHECKS FAILED')>=0?'failed':'running');
  console.log('LUNAR_SMOKE '+value);
 }
});
