window.scrollElementToEnd = function (element, {smooth = true} = {}) {
  if (element) {
    element.scrollIntoView({ behavior: smooth ? "smooth" : "instant" });
  }
};
