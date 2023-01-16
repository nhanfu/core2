function LoadJsComment() {
    var n = $(".rating-cmt-info-js").attr("data-js"),
        t = parseInt($(".rating-cmt-info-js").attr("siteid"));
    gl_getJsCmt || typeof cmtaddcommentclick == "undefined" && (gl_getJsCmt = !0, $.getScript(n).done(function () {
        console.log("get script cmt: " + n);
        setTimeout(function () {
            t === 1 && cmtInitEvent();
            reInitCmt2021();
            reConfigCmtParam(t);
            console.log(oParams)
        }, 200)
    }))
}

function reConfigCmtParam(n) {
    typeof oParams != "undefined" && window.location.origin.includes("staging") && (n == 1 ? (oParams = {
        sJsHome: "https://www.thegioididong.com/commentnew",
        sJsHomeU: "https://staging.thegioididong.com/commentmwg",
        sSiteName: "tgdd",
        sJsAjax: "https://www.thegioididong.com/commentnew/cmt/index",
        sStaticVersion: "977ce5003566dfb5058d954b0ea87d35",
        sGlobalTokenName: "core",
        bJsIsMobile: !1,
        "notification.notify_ajax_refresh": 2
    }, domainName = "http://www.thegioididong.com/commentnew", hostName = ".thegioididong.com") : n == 2 && (oParams.sJsAjax = window.location.origin + "/commentmwg/ajax/index", oParams.sJsHome = window.location.origin + "/commentmwg"), console.log("reConfig_cmt_: " + n))
}

function initUploadRatingImg() {
    console.log("initUploadRatingImg_mwg");
    var n = "",
        t = parseInt(rating_popup.find("#hdfSiteID").val());
    t == 1 ? n = getParam("sJsHomeU") + "/aj/Cmt/PostRatingImage" : t == 2 && (n = getParam("sJsHome") + "/aj/Home/PostImage");
    setTimeout(function () {
        rating_popup.find(".send-img").unbind();
        rating_popup.find(".send-img").on("click", function () {
            if (console.log("up Img Cmt"), rating_popup.find(".resRtImg li").length > 2) return alert("ÄĂ£ up load quĂ¡ sá»‘ áº£nh quy Ä‘á»‹nh. "), !1;
            $("#hdFileRatingUpload").click()
        });
        rating_popup.find("#hdFileRatingUpload").unbind();
        rating_popup.find("#hdFileRatingUpload").html5Uploader({
            postUrl: n,
            onClientLoadStart: function () {
                console.log("onClientLoadStart - upload rating img_mwg_")
            },
            onServerLoadStart: function () { },
            onServerProgress: function () { },
            onServerLoad: function () { },
            onSuccess: function (n) {
                var i = $.parseJSON(n.currentTarget.response),
                    t;
                if (i.status == -1) {
                    console.log(i);
                    alert("Xáº£y ra lá»—i, vui lĂ²ng thá»­ láº¡i sau. ");
                    return
                }
                console.log("onSuccess - upload RatingCmtIMG");
                t = "<li data-imgName='" + i.imageName + "'  >";
                typeof gl_rt_siteID != "undefined" && gl_rt_siteID == 2 && (t = "<li data-imgName='" + i.ImageName + "'  >");
                t += "<img src='" + i.imageUrl + "' />";
                t += "<span class='fbDelImg' onclick='rtDelImg(this)'>XĂ³a<\/span>";
                t += "<\/li>";
                rating_popup.find(".resRtImg").append(t);
                rating_popup.find(".resRtImg").removeClass("hide");
                getRtImg();
                uploadedFile++
            }
        })
    }, 1e3)
}

function getRtImg() {
    if (console.log("getRtImg_mwg"), rating_popup.find(".resRtImg li").length > 0) {
        var n = "";
        rating_popup.find(".resRtImg li").each(function () {
            var t = $(this).attr("data-imgname");
            t != null && t != "" && (n += t + "â•¬")
        });
        rating_popup.find(".hdfRtImg").val(n)
    } else rating_popup.find(".resRtImg li").length == 0 && rating_popup.find(".hdfRtImg").val("")
}

function rtDelImg(n) {
    rating_popup.find(n).parent().remove();
    getRtImg();
    rating_popup.find(".resRtImg li").length == 0 && rating_popup.find(".resRtImg").addClass("hide");
    uploadedFile--
}

function rtMoveFile(n, t) {
    var i, r, u;
    console.log("rtMoveFile_mwg");
    i = "";
    r = parseInt(rating_popup.find("#hdfSiteID").val());
    r == 1 ? i = getParam("sJsHomeU") + "/aj/Cmt/MoveFileRatingImage" : r == 2 && (i = getParam("sJsHome") + "/aj/Home/MoveFileRatingImage");
    n != null && n != "" && (u = {
        attachFile: rating_popup.find("#hdfRtImg").val(),
        commentID: t
    }, $.ajax({
        url: i,
        type: "POST",
        data: u,
        cache: !1,
        beforeSend: function () { },
        success: function (n) {
            (n != null || n != "") && console.log(n);
            hideloading()
        },
        error: function () {
            hideloading()
        }
    }))
}

function lazyImgCmt() {
    $("#comment img.lazy").each(function () {
        var n = $(this).attr("data-src");
        $(this).attr("src", n)
    })
}

function reInitCmt2021() {
    if (!gl_getJsCmtDmx) {
        var n = parseInt($(".rating-cmt-info-js").attr("siteid")),
            t = $(".rating-cmt-info-js").attr("data-isMobile") === "True" ? !0 : !1;
        n == 2 && ($(".midcmt .s_comment i").removeClass("icondmx-search").addClass("icon-search"), console.log("reInitCmt2021_"), gl_getJsCmtDmx = !0);
        $(".rating-cmt-info-js").attr("data-jsOvrCmt") != null && $(".rating-cmt-info-js").attr("data-jsOvrCmt") != "" && $.getScript($(".rating-cmt-info-js").attr("data-jsOvrCmt")).done(function () {
            console.log("getOverrideScript jsOverride-CMT");
            gl_getJsCmtDmx = !0
        })
    }
}

function RemoveBlockPopup(n, t, i) {
    if (isMobile) {
        t.remove();
        i.remove();
        let r = n.find(".rating-product__block");
        r.length == 0 ? n.remove() : r.length == 1 && (n.addClass("one-cmt"), n.find(".slide-cmt.carousel").addClass("box-cmt").removeClass("slide-cmt").removeClass("carousel"), r.find(".text-cmt").append(r.find(".rating-product__star")))
    } else {
        let r = $(".slide-cmt");
        r.trigger("remove.owl.carousel", r.find(".owl-item .item").index(t));
        i.remove();
        let f = r.data("max-item"),
            u = n.find(".rating-product__block");
        if (u.length == 0) {
            n.remove();
            return
        }
        u.length < f ? (u.each(function () {
            let n = $(this),
                t = n.find(".rating-product__star");
            n.find(".text-cmt").after(t.addClass("flex"));
            n.find(".text-cmt").after(t.addClass("flex"));
            n.find(".rating-product__star li i").removeClass("iconratingnew-star--medium").addClass("iconratingnew-star--big")
        }), r.data("owl.carousel").options.items = u.length) : u.length == f && (r.data("owl.carousel").options.stagePadding = 0, r.removeClass("stage-padding"));
        r.trigger("refresh.owl.carousel")
    }
}

function initRating() {
    var t, n;
    rooturl = document.location.host;
    let i = document.location.href;
    productUrl != null && (i = window.location.origin + productUrl);
    rating_popup.find("#hdfRtLink").val(i);
    gl_productID = parseInt(rating_popup.find("#hdfProductID").val());
    gl_rt_siteID = parseInt(rating_popup.find("#hdfSiteID").attr("value"));
    rating_popup.find("#hdfIsRatingPage").length > 0 && (gl_rt_isRatingPage = 1);
    isMobile !== undefined && (gl_rt_isMobile = isMobile);
    readCKLikeCmt();
    $(".selStr").length > 0 && ($(".selStr i").unbind(), $(".selStr i").hover(function () {
        var t, n;
        for ($(".selStr i").removeClass("active"), t = parseInt($(this).attr("id").replace("ss", "")), n = 0; n <= t; n++) $("#ss" + n).addClass("active")
    }, function () {
        $(".selStr i").removeClass("active")
    }));
    rating_popup.find(".ul-star li").unbind();
    rating_popup.find(".ul-star li").click(function () {
        var n, t;
        for (rating_popup.find(".ul-star li i").removeClass("active"), rating_popup.find(".ul-star li p").removeClass("active-slt"), n = parseInt($(this).attr("data-val")), t = 0; t < n; t++) rating_popup.find(".ul-star li i").eq(t).addClass("active");
        rating_popup.find(".ul-star li p").eq(n - 1).addClass("active-slt");
        rating_popup.find("#hdfStar").val(n);
        rating_popup.find(".ul-orslt, .read-assess-form").show()
    });
    rating_popup.find(".ul-orslt li .btn-assess").unbind();
    rating_popup.find(".ul-orslt li .btn-assess").click(function () {
        var n = $(this).attr("data-id"),
            t = $(this).attr("data-val");
        rating_popup.find(".criteriaID" + n + " .btn-assess").removeClass("checkact");
        $(this).addClass("checkact");
        rating_popup.find("#criteriaID" + n).attr("value", t)
    });
    rating_popup.find(".input .if [name=fRPhone]").unbind();
    rating_popup.find(".input .if [name=fRPhone]").keydown(function (n) {
        $.inArray(n.keyCode, [46, 8, 9, 27, 13, 110, 190]) !== -1 || n.keyCode === 65 && (n.ctrlKey === !0 || n.metaKey === !0) || n.keyCode >= 35 && n.keyCode <= 40 || (n.shiftKey || n.keyCode < 48 || n.keyCode > 57) && (n.keyCode < 96 || n.keyCode > 105) && n.preventDefault()
    });
    $(".ratingLst li .sttB").unbind();
    $(".ratingLst li .sttB").click(function () {
        var n = $(this).parents("li").attr("id");
        $("#" + n).find(".rcf").is(":visible") ? $("#" + n).find(".rcf").hide() : $("#" + n).find(".rcf").show()
    });
    $(".ratingLst .par").each(function () {
        var n = $(this).attr("id"),
            t;
        n = n.replace("r-", "");
        t = $(".ratingLst .rp-" + n).length;
        t > 0 && $(this).find(".cmtr").html(t + " tháº£o luáº­n")
    });
    t = "tgdd_fullname";
    gl_rt_siteID == 2 && (t = "dm_fullname");
    n = getCookie(t);
    n != null && n != "" && (n = Htmlentities(n), $(".ratingLst .ifrl").each(function () {
        $(this).find("span").html(decodeURI(n));
        $(this).find("a").html("Sá»­a tĂªn")
    }));
    $(".ratingLst li").length == 0 && $(".frtip .ipt").removeClass("hide");
    $(".click-use").unbind();
    $(".click-use").click(function (n) {
        n.preventDefault();
        $(this).toggleClass("act")
    });
    $(".frtip .toprt .c").each(function () {
        var n = $(this).find("strong").html();
        n = parseInt(n);
        n == 0 && ($(this).attr("onclick", ""), $(this).addClass("n"))
    });
    $(".srhRtTxt").length > 0 && $(".srhRtTxt").unbind().click(function () {
        $(".frtip .wrap_seasort").slideToggle()
    });
    $(".txt-agree").unbind();
    $(".txt-agree").click(function () {
        var n = parseInt($("#hdfIsShare").val());
        $(".txt-agree").removeClass("selected");
        n === 1 ? $("#hdfIsShare").val(0) : n === 0 && ($("#hdfIsShare").val(1), $(".txt-agree").addClass("selected"))
    });
    $(".icon-dots").unbind();
    $(".icon-dots").on("click", function (n) {
        n.preventDefault();
        $(this).next(".comment__item .txt-dots").fadeToggle(300)
    });
    $(".txt-dots").unbind();
    $(".txt-dots").click(function () {
        $(".show-history").fadeIn(400);
        $("body").css({
            overflow: "hidden"
        })
    });
    $(".close-history,.close-history-href").unbind();
    $(".close-history,.close-history-href").click(function () {
        $(".show-history").fadeOut(400);
        $("body").css({
            overflow: "scroll"
        });
        $(".comment__item .txt-dots").hide();
        gl_rt_isMobile || ($(".locationbox__overlay").hide(), $(".bgback").removeClass("showbg"))
    });
    $(".c-checkitem").unbind();
    $(".c-checkitem").on("click", function () {
        $(this).toggleClass("act-check");
        var n = parseInt($(this).attr("data-val"));
        n == 0 ? $(this).attr("data-val", 1) : $(this).attr("data-val", 0);
        ratingCmtList(1, 0)
    });
    $(".boxsort__click-show").unbind();
    $(".boxsort__click-show").click(function () {
        $(this).toggleClass("active");
        $(".boxsort__list").fadeToggle(200)
    });
    $(".boxsort__list li").unbind();
    $(".boxsort__list li").click(function () {
        $(".boxsort").attr("data-val", $(this).attr("data-val"));
        $(".boxsort__click-show").html($(this).html());
        $(".boxsort__click-show").toggleClass("active");
        $(".boxsort__list").fadeToggle(200);
        ratingCmtList(1, 0)
    });
    calSizeThumpRtImg();
    $(".rating-img-rd").click(function () {
        gl_rt_isMobile ? ($(".gallery").addClass("showGlr"), $("body").css({
            overflow: "hidden"
        })) : ($(".gallery").addClass("showGlr"), $("body").css({
            overflow: "hidden"
        }), $(".bgback").addClass("showbg"), $(".locationbox__overlay").show())
    });
    $(".gallery-close").click(function () {
        gl_rt_isMobile ? ($(".gallery").removeClass("showGlr"), $("body").css({
            overflow: "scroll"
        })) : ($(".bgback").removeClass("showbg"), $(".show-comment").removeClass("blockshow").addClass("hide"), $(".gallery").removeClass("showGlr"), $(".locationbox__overlay").hide())
    });
    $(".gallery__tab a").click(function (n) {
        n.preventDefault();
        $(".gallery__tab a").removeClass("act");
        var t = $(this).attr("href");
        t = t.substr(1);
        $(this).addClass("act");
        $(".gallery__content li").each(function () {
            $(this).hasClass(t) || t == "all" ? $(this).removeClass("hide") : $(this).addClass("hide")
        })
    })
}

function initRtSlide() {
    if (gl_rt_isMobile) console.log("slide m_111"), $(".show-comment-main").owlCarousel({
        lazyLoad: !0,
        items: 1,
        loop: !0,
        dots: !1,
        autoplay: !1,
        nav: !0,
        autoHeight: !0
    });
    else {
        console.log("slide d_1");
        var n = $("#cmt_sync1"),
            t = $("#cmt_sync2");
        n.owlCarousel({
            lazyLoad: !0,
            items: 1,
            singleItem: !0,
            slideSpeed: 1e3,
            nav: !0,
            dots: !1,
            navigationText: ["", ""],
            pagination: !1,
            responsiveRefreshRate: 200
        });
        n.on("changed.owl.carousel", function (n) {
            console.log("slide1 change: " + n.item.index);
            $("#cmt_sync2").trigger("to.owl.carousel", n.item.index)
        });
        t.owlCarousel({
            items: 1,
            itemsDesktop: [1199, 1],
            itemsDesktopSmall: [979, 1],
            pagination: !1,
            responsiveRefreshRate: 100
        });
        t.on("changed.owl.carousel", function (n) {
            console.log("slide2 change: " + n.item.index);
            $("#cmt_sync1").trigger("to.owl.carousel", n.item.index)
        })
    }
}

function getOverrideScript() {
    if (gl_isGetScript) return !1;
    gl_rt_isRatingPage == 1 ? typeof initUploadRatingImg == "function" && (console.log("getOverrideScript jsOvrCmt_detail"), initUploadRatingImg(), gl_isGetScript = !0) : (typeof initUploadRatingImg == "function" && (console.log("getOverrideScript jsOvrCmt_detail"), initUploadRatingImg()), gl_isGetScript = !0)
}

function showRatingCmtChild(n) {
    var t = n.replace("r-", ""),
        i, r;
    $(".rp-" + t).removeClass("hide");
    $(".rr-" + t).removeClass("hide");
    i = "tgdd_fullname";
    gl_rt_siteID == 2 && (i = "dm_fullname");
    r = getCookie(i);
    (r == null || r == "") && $(".rr-" + t + " .ifrl").removeClass("hide").addClass("hide")
}

function likeRating(n) {
    var t = {
        id: n
    },
        i = window.location.origin + "/Rating/LikeRating/";
    $.ajax({
        url: i,
        type: "POST",
        data: t,
        cache: !1,
        beforeSend: function () { },
        success: function (t) {
            var i, r;
            if (t != null || t != "") try {
                i = parseInt($("#r-" + n + " .click-like").attr("data-like"));
                i++;
                $("#r-" + n).attr("data-like", i);
                r = "<i class='icondetail-like'><\/i> Há»¯u Ă­ch (" + i + ")";
                $("#r-" + n + " .click-like").html(r);
                $("#r-" + n + " .click-like").attr("href", "javascript:;");
                updateCKLikeCmt(n)
            } catch (u) {
                console.log(u)
            }
        },
        error: function () { }
    })
}

function updateCKLikeCmt(n) {
    var i = "tgdd_cmtlike",
        t;
    if (gl_rt_siteID == 2 && (i = "dmx_cmtlike"), t = getCookie(i), t !== undefined && t !== null && t !== "") {
        for (var u = !0, r = 0, f = t.split("-"), r = 0; r < f.length; r++) parseInt(n) === parseInt(f[r]) && (u = !1);
        u && (t = t + "-" + n, CreateCookie(i, t, 1))
    } else CreateCookie(i, n, 1)
}

function readCKLikeCmt() {
    var r = "tgdd_cmtlike",
        t, n, i;
    if (gl_rt_siteID === 2 && (r = "dmx_cmtlike"), t = getCookie(r), t !== undefined && t !== null && t !== "")
        for (n = 0, i = t.split("-"), n = 0; n < i.length; n++) rating_popup.find("#r-" + i[n]).length > 0 && (rating_popup.find("#r-" + i[n] + " .click-like").attr("href", "javascript:;"), rating_popup.find("#r-" + i[n] + " .click-like").html("<i class='icondetail-like'><\/i> Há»¯u Ă­ch (1)"))
}

function ratingMore(n, t) {
    var i = 5,
        r, u;
    t != null && t >= 0 && (i = t, gl_rtCurStar = t);
    r = {
        productid: gl_productID,
        page: n,
        score: i
    };
    u = "/aj/Rating/RatingCommentList/";
    POSTAjax(u, r, function () { }, function (t) {
        if (t != null || t != "") try {
            $(".ratingMore").remove();
            n == 1 ? $(".ratingLst").html(t) : $(".ratingLst").append(t);
            initRating();
            $("img.lazy").trigger("sporty")
        } catch (i) { }
    }, function () { }, !0)
}

function submitRatingComment() {
    if (!gl_sendRating) {
        if (gl_sendRating = !0, !validateRating()) {
            gl_sendRating = !1;
            return
        }
        var n = rating_popup.find(".frtip"),
            t = n.serialize(),
            i = window.location.origin + "/Rating/SubmitRatingComment/";
        $.ajax({
            url: i,
            type: "POST",
            data: t,
            cache: !1,
            beforeSend: function () {
                showloading()
            },
            success: function (t) {
                if (t != null || t != "") try {
                    initRating();
                    gl_sendRating = !1;
                    var i = rating_popup.find("#hdfRtImg").val();
                    if (t.res == 1) {
                        parseInt(t.resCmt) > 0 && i != null && i != "" && (console.log("rtMoveFile: " + i), rtMoveFile(i, t.resCmt), hideloading());
                        let r = n.find(".box-cmt-popup .info-pro"),
                            u;
                        u = gl_rt_siteID == 1 ? {
                            name: r.data("name"),
                            id: r.data("id"),
                            price: r.data("price"),
                            brand: r.data("brand"),
                            category: r.data("category"),
                            reviewWithPhoto: i != null && i != "" ? "Yes" : "No",
                            anonymousReview: "No",
                            productName: r.data("name"),
                            rateTotal: n.find("input[name=hdfStar]").val(),
                            productPrice: r.data("price"),
                            dimension33: "",
                            dimension34: "",
                            dimension35: "",
                            dimension36: r.data("price"),
                            dimension43: n.find("input[name=hdfStar]").val()
                        } : {
                            name: r.data("name"),
                            id: r.data("id"),
                            price: r.data("price"),
                            brand: r.data("brand"),
                            category: r.data("category"),
                            reviewWithPhoto: i != null && i != "" ? "Yes" : "No",
                            anonymousReview: "No",
                            productName: r.data("name"),
                            rateTotal: n.find("input[name=hdfStar]").val(),
                            productPrice: r.data("price"),
                            dimension50: "",
                            dimension51: "",
                            dimension52: "",
                            dimension53: r.data("price"),
                            dimension60: n.find("input[name=hdfStar]").val()
                        };
                        let f = {
                            event: "formSubmissionSuccess",
                            formSubmissionSuccess: u
                        };
                        dataLayer.push(f);
                        alert("ÄĂ¡nh giĂ¡ cá»§a báº¡n sáº½ Ä‘Æ°á»£c há»‡ thá»‘ng kiá»ƒm duyá»‡t. Xin cĂ¡m Æ¡n.");
                        console.log("resetRating: " + i);
                        RemoveBlockPopup(rating_container, rating_block, rating_popup);
                        $(".locationbox__overlay").hide();
                        closeFeedback();
                        hideloading()
                    } else t.res == 0 && alert(t.mes);
                    hideloading()
                } catch (r) {
                    gl_sendRating = !1;
                    hideloading()
                }
            },
            error: function () {
                gl_sendRating = !1;
                hideloading()
            }
        })
    }
}

function ratingRelply(n) {
    var i, r, t, u, f;
    if (gl_sendRating) return !1;
    if (gl_sendRating = !0, i = $.trim($(".rr-" + n).find("input").val()), i == null || i == "") return gl_sendRating = !1, alert("Vui lĂ²ng nháº­p ná»™i dung cáº§n tháº£o luáº­n"), !1;
    if (r = "tgdd_fullname", gl_rt_siteID == 2 && (r = "dm_fullname"), t = getCookie(r), t == null || t == "") return gl_sendRating = !1, showReplyConfirmPopup(), !1;
    u = {
        productid: gl_productID,
        commentid: n,
        content: $(".rr-" + n).find("input").val(),
        name: t,
        siteID: gl_rt_siteID
    };
    f = window.location.origin + "/Rating/SubmitRatingReply/";
    $.ajax({
        url: f,
        type: "POST",
        data: u,
        cache: !1,
        beforeSend: function () {
            $("#dlding").show()
        },
        success: function (n) {
            if (n != null || n != "") try {
                n.res == 1 && ($(".ratingLst .reply input").val(""), alert("Tháº£o luáº­n cá»§a báº¡n sáº½ Ä‘Æ°á»£c há»‡ thá»‘ng kiá»ƒm duyá»‡t. Xin cĂ¡m Æ¡n."));
                gl_sendRating = !1
            } catch (t) { }
            hideloading()
        },
        error: function () {
            gl_sendRating = !1;
            hideloading()
        }
    })
}

function rCmtEditName() {
    showReplyConfirmPopup()
}

function clearBorder() {
    $(".frtip .ct").removeClass("borderWn");
    $(".frtip .if input").removeClass("borderWn")
}

function validateRating() {
    var n, i, t;
    return (clearBorder(), rating_popup.find(".lbMsgRt").removeClass("hide"), n = $.trim(rating_popup.find(".input [name=fRContent]").val()), i = parseInt(rating_popup.find("#hdfStar").val()), i == 0) ? (rating_popup.find(".lbMsgRt").html("Báº¡n chÆ°a Ä‘Ă¡nh giĂ¡ Ä‘iá»ƒm sao, vui lĂ²ng Ä‘Ă¡nh giĂ¡."), paddingForm(), !1) : (n == null || n == "") && confirm("Chá» Ä‘Ă£! Ná»™i dung chÆ°a Ä‘Æ°á»£c nháº­p, báº¡n sáºµn lĂ²ng nháº­p thĂªm chá»©?") ? (rating_popup.find(".frtip .ct").focus(), !1) : (n = $.trim(rating_popup.find(".input [name=fRName]").val()), n == null || n == "") ? (rating_popup.find(".lbMsgRt").html("Vui lĂ²ng nháº­p há» tĂªn"), rating_popup.find(".input [name=fRName]").addClass("borderWn"), paddingForm(), !1) : (n = $.trim(rating_popup.find(".input [name=fRPhone]").val()), n == null || n == "") ? (rating_popup.find(".lbMsgRt").html("Vui lĂ²ng nháº­p sá»‘ Ä‘iá»‡n thoáº¡i."), rating_popup.find(".input [name=fRPhone]").addClass("borderWn"), paddingForm(), !1) : rating_popup.find(".ul-orslt li").length > 0 && (t = rating_popup.find(".ul-orslt .checkact").length, t > 0 && t != rating_popup.find(".ul-orslt li").length) ? (rating_popup.find(".lbMsgRt").html("Vui LĂ²ng chá»n táº¥t cáº£ cĂ¡c tiĂªu chĂ­"), paddingForm(), !1) : (rating_popup.find(".lbMsgRt").removeClass("hide").addClass("hide"), !0)
}

function paddingForm() {
    gl_rt_isMobile && $(".read-assess-form").css("padding-bottom", `${$(".submit-container").outerHeight()}px`)
}

function rSelGender(n, t) {
    $(".rCfmInfo .cgd i").removeClass("icondetail-radcheck").addClass("icondetail-rad");
    $(n).find("i").removeClass("icondetail-rad").addClass("icondetail-radcheck");
    $(".rCfmInfo").attr("data-gender", t)
}

function resetRating() {
    console.log("rs rt");
    clearBorder();
    rating_popup.find(".ul-star li i").removeClass("active");
    rating_popup.find(".ul-star li p").removeClass("active-slt");
    rating_popup.find(".ul-orslt .btn-assess").removeClass("checkact");
    rating_popup.find("#hdfStar").val("0");
    rating_popup.find(".input [name=fRContent]").val("");
    rating_popup.find(".input [name=fRName]").val("");
    rating_popup.find(".input [name=fRPhone]").val("");
    rating_popup.find(".input [name=fREmail]").val("");
    rating_popup.find(".rsStar").html("");
    rating_popup.find(".rsStar").addClass("hide");
    rating_popup.find(".lbMsgRt").html("");
    rating_popup.find(".resRtImg").html("");
    rating_popup.find(".resRtImg").addClass("hide");
    rating_popup.find("#hdfRtImg").val("")
}

function countTxtRating() {
    var n = $(".frtip textarea").val().length,
        t;
    n > 0 && n < 80 ? (t = n + " kĂ½ tá»± (tá»‘i thiá»ƒu 80)", $(".mintext").html(t)) : $(".mintext").html("")
}

function getImgRating() {
    if ($(".resImg li").length > 0) {
        var n = "";
        $(".resImg li").each(function () {
            var t = $(this).attr("data-imgname");
            t != null && t != "" && (n += t + "â•¬")
        });
        $(".hdfRatingImg").val(n)
    }
}

function checkPopupRating() {
    setTimeout(function () {
        var t = getUrlParameter("popuprating"),
            n, i, r;
        t != null && t != "" ? (console.log("popuprating: " + t), n = t.split("-"), n.length > 2 && n[0] == 1 && (rating_popup.find(".frtip").remove(), rating_popup.find(".fsrt").remove(), rating_popup.find(".wrap_fdback").removeClass("hide"), rating_popup.find(".wrap_fdback form.input").show(), i = n[1], r = n[2], i != null && i != "" && (rating_popup.find(".wrap_fdback .input [name='fRName']").val(i), rating_popup.find(".wrap_fdback .input [name='fRPhone']").val(r)))) : rating_popup.find(".wrap_fdback").remove()
    }, 1e3)
}

function ratingSearch() {
    var n, t, i, r;
    if (console.log("ratingSearch0618_m"), n = $(".cmtKey").val(), n = $.trim(n), n === null || n === "" || n.length < 3) return !1;
    t = $(".rtpLnk").attr("data-orgLnk") + "?key=" + encodeURI(n);
    $(".rtpLnk").prop("href", t);
    gl_sendRating = !0;
    i = window.location.origin + "/Rating/SearchRating/";
    r = {
        sKey: n,
        productID: gl_productID
    };
    $.ajax({
        url: i,
        type: "POST",
        data: r,
        cache: !1,
        beforeSend: function () { },
        success: function (n) {
            (n !== null || n !== "") && ($(".rFound").remove(), $(".rtPage .ratingLst").remove(), $(".rtPage .pgrc").remove(), gl_rt_isMobile ? $(".rtPage .boxsort").after(n) : $(".rtPage .content-wrap .rtFilter").after(n), gl_sendRating = !1);
            $(".frtip .wrap_seasort").hide()
        },
        error: function () {
            gl_sendRating = !1
        }
    })
}

function showRtHis(n) {
    $(".rtHislbl").addClass("hide");
    $("#r-" + n + " .rtHislbl").removeClass("hide")
}

function rtHis(n) {
    var t = {
        cmtID: n,
        productID: gl_productID
    },
        i = window.location.origin + "/Rating/GetRatingHistory/";
    $(".wrap_His .hList").html("");
    $.ajax({
        url: i,
        type: "POST",
        data: t,
        cache: !1,
        beforeSend: function () { },
        success: function (n) {
            (n !== null || n !== "") && ($(".ratingLst").after(n), $(".show-history").fadeIn(400), gl_rt_isMobile || ($(".bgback").addClass("showbg"), gl_rt_isRatingPage || $(".locationbox__overlay").show()), initRating())
        },
        error: function () { }
    })
}

function closePopupRating() {
    $(".wrap_wrtp").removeClass("hide").addClass("hide")
}

function showRtImgListPop() {
    console.log("showRtImgListPop_");
    gl_rt_isMobile ? ($(".gallery").addClass("showGlr"), $("body").css({
        overflow: "hidden"
    })) : ($(".gallery").addClass("showGlr"), $("body").css({
        overflow: "hidden"
    }), $(".bgback").addClass("showbg"), $(".locationbox__overlay").show());
    $(".show-comment").removeClass("blockshow")
}

function getFileUpload() {
    gl_isFeedbackLoad || $.getScript("/lib/jquery-upload/jquery.html5uploader.min.js?v=20220613").done(function () {
        gl_isFeedbackLoad = !0;
        initUploadRating()
    })
}

function getUrlParameter(n) {
    for (var u = decodeURIComponent(window.location.search.substring(1)), r = u.split("&"), t, i = 0; i < r.length; i++)
        if (t = r[i].split("="), t[0] === n) return t[1] === undefined ? !0 : t[1]
}

function ratingCmtList(n, t) {
    var i, r, u, f, e, o;
    console.log("ratingCmtList_");
    $(".cmtKey").val("");
    $(".rFound").length > 0 && $(".rFound").remove();
    i = gl_rtCurStar;
    t !== null && t >= 0 ? (i = t, gl_rtCurStar = t) : gl_rtCurStar >= 0 && (t = gl_rtCurStar);
    gl_rt_currentPage = parseInt(n);
    gl_rtCurStar = parseInt(t);
    r = 0;
    $(".cIsImg").length > 0 && (r = parseInt($(".cIsImg").attr("data-val")));
    r === 1 && (gl_rt_currentImg = !0);
    u = 0;
    $(".cIsBuy").length > 0 && (u = parseInt($(".cIsBuy").attr("data-val")));
    f = 0;
    $(".boxsort").length > 0 && (f = parseInt($(".boxsort").attr("data-val")));
    gl_rt_currentOrder = parseInt($(".frtip .ftR .o").attr("data-order"));
    gl_rt_currentPage = n;
    e = {
        productid: gl_productID,
        page: n,
        score: i,
        iIsImage: r,
        iIsBuy: u,
        iOrder: f
    };
    o = window.location.origin + "/Rating/RatingCommentList/";
    $(".frtip .ftR .s").removeClass("act");
    $(".frtip .ftR .s" + t).addClass("act");
    showloading();
    $.ajax({
        url: o,
        type: "POST",
        data: e,
        cache: !1,
        beforeSend: function () {
            $("#dlding").show()
        },
        success: function (n) {
            if (n != null || n != "") try {
                $(".rtPage .ratingLst").remove();
                $(".rtPage .pgrc").remove();
                gl_rt_isMobile ? $(".rtPage .boxsort").after(n) : $(".rtPage .content-wrap .rtFilter").after(n);
                $(".rtPage .filter-choose").length > 0 && ($(".rtPage .filter-choose a").removeClass("active"), $(".rtPage .filter-choose .rtF" + i).addClass("active"));
                $(".rtPage .list .rtQRp").remove();
                $(".wrap_slide .rat").remove();
                initRating();
                reloadRatingUrl();
                reInitRatingImg();
                $("img.lazy").trigger("sporty");
                hideloading();
                $("html, body").animate({
                    scrollTop: $(".boxsort").offset().top - 20
                }, "slow")
            } catch (t) {
                console.log(t);
                hideloading()
            }
        },
        error: function () { }
    })
}

function showloading() {
    $(".loadingcover").show()
}

function hideloading() {
    $(".loadingcover").hide()
}

function reloadRatingUrl() {
    removeURLParameter("s");
    removeURLParameter("i");
    removeURLParameter("o");
    removeURLParameter("p");
    gl_rtCurStar !== null && gl_rtCurStar > 0 && window.history.replaceState("", "", updateURLParameter(window.location.href, "s", gl_rtCurStar));
    gl_rt_currentImg !== null && gl_rt_currentImg && window.history.replaceState("", "", updateURLParameter(window.location.href, "i", "1"));
    gl_rt_currentOrder !== null && gl_rt_currentOrder > 0 && window.history.replaceState("", "", updateURLParameter(window.location.href, "o", gl_rt_currentOrder));
    gl_rt_currentPage !== null && gl_rt_currentPage > 0 && window.history.replaceState("", "", updateURLParameter(window.location.href, "p", gl_rt_currentPage))
}

function reInitRatingImg() {
    $(".ratingLst .isBuy .rat img").attr("onclick", "");
    $(".ratingLst .isBuy .rat img").unbind();
    $(".ratingLst .isBuy .rat img").click(function () {
        console.log("img click_");
        $(".wrap_slide").removeClass("hide");
        var n = $(this).attr("data-id");
        $(".wrap_slide .item img").each(function (t) {
            $(this).attr("data-id") === n && setTimeout(function () {
                var n = $(".owl-carousel");
                n.trigger("owl.goTo", t)
            }, 100)
        })
    });
    $(".wrap_fullImg img").unbind();
    $(".wrap_fullImg img").click(function () {
        console.log("img click_2");
        $(".wrap_fullImg").removeClass("hide").addClass("hide");
        $(".wrap_slide").removeClass("hide");
        var n = $(this).attr("data-id");
        $(".wrap_slide .item img").each(function (t) {
            $(this).attr("data-id") === n && setTimeout(function () {
                var n = $(".owl-carousel");
                n.trigger("owl.goTo", t)
            }, 100)
        })
    })
}

function goToRSlideByAttID(n) {
    $(".rtAtt-" + n).click()
}

function goToRSlide(n) {
    initRtSlide();
    gl_rt_isMobile ? ($(".show-comment").removeClass("hide").addClass("blockshow"), $("body").css({
        overflow: "hidden"
    }), $("html").css({
        overflow: "hidden"
    })) : ($(".gallery").removeClass("showGlr"), $(".show-comment").removeClass("hide").addClass("blockshow"), $(".bgback").addClass("showbg"), $("body").css({
        overflow: "hidden"
    }), gl_rt_isRatingPage || $(".locationbox__overlay").show());
    $(".locationbox__overlay").show();
    var n = parseInt(n);
    n > 0 && (gl_rt_isMobile ? $(".show-comment-main").trigger("to.owl.carousel", n - 1) : $("#cmt_sync1").trigger("to.owl.carousel", n - 1))
}

function updateURLParameter(n, t, i) {
    var e = "",
        r = n.split("?"),
        h = r[0],
        o = r[1],
        f = "",
        u, s;
    if (o)
        for (r = o.split("&"), u = 0; u < r.length; u++) r[u].split("=")[0] != t && (e += f + r[u], f = "&");
    return s = f + "" + t + "=" + i, h + "?" + e + s
}

function removeURLParameter(n) {
    var t = document.location.href,
        u = t.split("?"),
        r;
    if (u.length >= 2) {
        var f = u.shift(),
            e = u.join("?"),
            o = encodeURIComponent(n) + "=",
            i = e.split(/[&;]/g);
        for (r = i.length; r-- > 0;) i[r].lastIndexOf(o, 0) !== -1 && i.splice(r, 1);
        t = f + "?" + i.join("&");
        window.history.pushState("", document.title, t)
    }
    return t
}

function showInputRating(n, t) {
    rating_container = $(".rating-product");
    rating_block = $(t).closest(".rating-product__block");
    rating_popup = rating_container.find(".rating-product__popup[data-index=" + rating_block.data("index") + "]");
    productUrl = null;
    productUrl = rating_block.data("url");
    LoadJsComment();
    initRating();
    checkPopupRating();
    hideRtContentText();
    gl_rt_isMobile ? rating_popup.find(".read-assess").removeClass("hide").addClass("blockshow") : (rating_popup.find(".read-assess").removeClass("hide").addClass("showR"), $(".locationbox__overlay").show());
    parseInt(n) > 0 && rating_popup.find(".ul-star li").each(function () {
        parseInt($(this).attr("data-val")) <= n && $(this).click()
    });
    getOverrideScript()
}

function hideInputRating() {
    rating_popup.find(".fbDelImg").click();
    rating_popup.find("#hdfStar").val() > 0 ? confirm("Chá» Ä‘Ă£! Báº¡n chÆ°a gá»­i Ä‘Ă¡nh giĂ¡, báº¡n cĂ³ cĂ³ muá»‘n gá»­i Ä‘i khĂ´ng?") ? submitRatingComment() : closeRatingForm() : closeRatingForm()
}

function closeRatingForm() {
    rating_popup.find(".frtip .input").fadeOut();
    rating_popup.find(".frtip .cipRating").removeClass("hide").addClass("hide");
    rating_popup.find(".frtip .sRt").removeClass("hide");
    rating_popup.find(".lnkFbk").removeClass("hide");
    rating_popup.find(".frtip .fsrt a").attr("href", "javascript:showInputRating()");
    rating_popup.find(".frtip .fsrt a span").html("Gá»­i Ä‘Ă¡nh giĂ¡ cá»§a báº¡n");
    gl_rt_isMobile ? rating_popup.find(".read-assess").removeClass("blockshow").addClass("hide") : (rating_popup.find(".read-assess").removeClass("showR").addClass("hide"), $(".locationbox__overlay").hide())
}

function closeRtGallery() {
    $(".bgback").removeClass("showbg");
    $(".show-comment").removeClass("blockshow").addClass("hide");
    $("body").css({
        overflow: ""
    });
    $("html").css({
        overflow: ""
    });
    gl_rt_isMobile || gl_rt_isRatingPage ? $(".locationbox__overlay").length > 0 && $(".locationbox__overlay").hide() : $(".locationbox__overlay").hide()
}

function showReplyConfirmPopup() {
    var r = "tgdd_fullname",
        u = "tgdd_email",
        f = "tgdd_gender",
        e = "tgdd_phone";
    gl_rt_siteID == 2 && (r = "dm_fullname", u = "dm_email", f = "dm_gender", e = "dm_phone");
    var n = getCookie(r),
        t = getCookie(u),
        i = getCookie(e),
        o = getCookie(f);
    n != null && n != "" && (n = Htmlentities(n), $(".cfmUserName").val(n));
    t != null && t != "" && $(".cfmUserEmail").val(t);
    i != null && i != "" && $(".cfmPhone").val(i);
    parseInt(o) == 1 && $(".c_male").click();
    parseInt(o) == 2 && $(".c_female").click();
    gl_rt_isMobile ? $(".rRepPopup").removeClass("hide").addClass("blockshow") : ($(".rRepPopup").removeClass("hide").addClass("blockshow"), $(".locationbox__overlay").show())
}

function rCmtConfirmUser() {
    var u = "tgdd_fullname",
        f = "tgdd_email",
        e = "tgdd_gender",
        o = "tgdd_phone",
        n, t, i, r;
    gl_rt_siteID == 2 && (u = "dm_fullname", f = "dm_email", e = "dm_gender", o = "dm_phone");
    n = $(".cfmUserName").val();
    n != null && n != "" && (n = Htmlentities(n), CreateCookie(u, n));
    t = $(".cfmUserEmail").val();
    t != null && t != "" && CreateCookie(f, t);
    i = $(".cfmPhone").val();
    i != null && i != "" && CreateCookie(o, i);
    r = parseInt($(".rCfmInfo").attr("data-gender"));
    (r == 1 || r == 2) && CreateCookie(e, r);
    $(".ifrl span").html(n);
    $(".ifrl").removeClass("hide");
    $(".ifrl a").html("Sá»­a tĂªn");
    hideReplyConfirmPopup()
}

function hideReplyConfirmPopup() {
    $(".rRepPopup").removeClass("blockshow").addClass("hide");
    gl_rt_isMobile || $(".locationbox__overlay").hide()
}

function hideRtContentText() {
    var n = 310;
    gl_rt_isRatingPage != 1 || gl_rt_isMobile || (n = 600);
    rating_popup.find(".ratingLst .comment-content").each(function () {
        if ($(this).find(".cmt-txt").length > 0 && $(this).find(".cmt-txt").html().length > n) {
            $(this).find(".cmt-txt").addClass("hideRtCt");
            var t = $(this).parent().attr("id");
            $(this).after("<span class='sRtXt' onclick='showFullRtCt(this)'>Xem tiáº¿p â–¾ <\/span>")
        }
    });
    rating_popup.find(".show-comment-main .comment-content").each(function () {
        if ($(this).find(".cmt-txt").length > 0 && $(this).find(".cmt-txt").html().length > 120) {
            $(this).find(".cmt-txt").addClass("hideRtCt");
            var n = $(this).attr("data-id");
            $(this).after('<span class="sRtXt sRtXt' + n + '" onclick="showFullRtCtPop(\'' + n + "')\">Xem tiáº¿p â–¾ <\/span>")
        }
    })
}

function showFullRtCt(n) {
    var t = $(n).parent().attr("id");
    $("#" + t + " .cmt-txt").removeClass("hideRtCt");
    $(n).remove()
}

function showFullRtCtPop(n) {
    $(".crtipu-" + n + " .cmt-txt").removeClass("hideRtCt");
    $(".sRtXt" + n).remove()
}

function calSizeThumpRtImg() {
    if (gl_rt_isMobile && window.screen.width >= 320 && window.screen.width <= 640) {
        var n = window.screen.width / 5 - 8;
        $(".rtPage .rating-img .rating-img-list .js-Showcmt img").each(function () {
            $(this).css("width", n + "px");
            $(this).css("height", n + "px")
        })
    }
}

function cmtValidateEmail(n) {
    return /^([\w-]+(?:\.[\w-]+)*)@((?:[\w-]+\.)*\w[\w-]{0,66})\.([a-z]{2,6}(?:\.[a-z]{2})?)$/i.test(n)
}

function Htmlentities(n) {
    return n.replace(/[\u00A0-\u9999<>\&]/g, function (n) {
        return "&#" + n.charCodeAt(0) + ";"
    })
}

function CreateCookie(n, t, i) {
    var r = new Date,
        u;
    r.setDate(r.getDate() + i);
    u = escape(t) + (i == null ? "" : "; visited=true; path=/; domain=" + rooturl + "; expires=" + r.toUTCString() + ";");
    document.cookie = n + "=" + u
}

function CreateCookieWithHour(n, t, i) {
    var r = new Date,
        u;
    r.setMinutes(r.getMinutes() + i);
    u = escape(t) + (i == null ? "" : "; visited=true; path=/; domain=" + rooturl + "; expires=" + r.toUTCString() + ";");
    document.cookie = n + "=" + u
}

function Delete_Cookie(n, t, i) {
    getCookie(n) && (document.cookie = n + "=" + (t ? ";path=" + t : "") + (i ? ";domain=" + i : "") + ";expires=Thu, 01 Jan 1970 00:00:01 GMT")
}

function SetAllContentAttr() {
    var n = $(".option-sg a.active");
    n.each(function () {
        let n = $(this),
            t = n.closest(".daily-sg").find(".block-product__content:not([data-is-recommend-tab]):not([data-campaign]):not([data-group])");
        t != null && (n.data("is-recommend-tab") != null && t.attr("data-is-recommend-tab", n.data("is-recommend-tab")), n.data("campaign") != null && t.attr("data-campaign", n.data("campaign")), n.data("group") != null && t.attr("data-group", n.data("group")))
    })
}

function InitCarousel() {
    if ($(".home-slider.big-campaign").length > 0) {
        let n = $(".contain-banner .slider-banner a > img").height();
        $(".contain-banner .slider-banner").css("max-height", n)
    }
    var n = $(".slider-banner").owlCarousel({
        items: 1,
        loop: !0,
        dots: !0,
        margin: 5,
        nav: !1,
        autoplay: !0,
        autoplayTimeout: 1e4,
        autoplayHoverPause: !0
    });
    n.on("changed.owl.carousel", function () { })
}

function InitOwlCarousel() {
    $(".slider-banner").each(function () {
        var t = 3,
            n = 30;
        $(this).hasClass("home-top") && (n = 10, t = 2);
        $(this).parents(".another-slider").length && (n = 0);
        $(this).owlCarousel({
            items: t,
            loop: !1,
            dots: !1,
            margin: n,
            nav: !0,
            autoplay: !0,
            autoplayTimeout: 5e3,
            autoplayHoverPause: !0,
            slideBy: "page",
            rewind: !0
        })
    });
    InitInviteRatingOwlCarousel($(".slide-cmt"));
    sliderPromoInitCarousel();
    $(".trademark-slider").owlCarousel({
        items: 3,
        margin: 10,
        nav: !0,
        slideBy: "page",
        dots: !1,
        rewind: !0
    })
}

function ConvertToArray(n) {
    let t = [];
    if (n != null) t = JSON.parse("[" + n.toString().replace(/[\u200B-\u200D\uFEFF]/g, "") + "]");
    return t
}

function sliderPromoInitCarousel() {
    owlSliderPromo = $(".slider-promo").owlCarousel({
        nav: !0,
        items: 5,
        nav: !0,
        dots: !1,
        slideBy: "page",
        rewind: !0,
        smartSpeed: 100
    })
}

function InitInviteRatingOwlCarousel(n) {
    let t = n.find(".rating-product__block").length,
        i = n.data("max-item"),
        r = 0;
    r = t < i ? t : i;
    let u = 70;
    n.on("translate.owl.carousel", function () {
        setTimeout(function () {
            let t = n.find(".owl-item");
            t.last().hasClass("active") ? n.removeClass("padding-right") : n.addClass("padding-right");
            t.first().hasClass("active") ? n.removeClass("padding-left") : (n.addClass("padding-left"), n.find(".owl-stage").css("padding-left", `${u * 2}px`))
        }, 1)
    }).owlCarousel({
        items: r,
        slideBy: "page",
        rewind: !0,
        dots: !1,
        margin: 10,
        nav: !0,
        lazyLoad: !0,
        stagePadding: n.data("has-half-item") ? u : 0
    })
}

function ShowHideStickyButton() {
    let t = $(".bg-tophome"),
        n = $(".sticky-button");
    t.isInViewport() ? n.hide() : sessionStorage.getItem(keyStickyButton) == null && n.show()
}

function GetPopupRemindGiftVoucher() {
    var n = getCookie(getRemindGiftVoucherCookieName());
    console.log("CK_NAME:", getRemindGiftVoucherCookieName());
    n != "" && n != undefined && n != null && $.ajax({
        url: "/cart/api/Common/getpopupremindgiftvoucher",
        method: "GET",
        beforeSend: function () { },
        success: function (n) {
            $("body").append(n)
        },
        complete: function () { }
    })
}

function flashsaleCountdown() {
    var o = $(".flashsale-block .endtime").data("countdown"),
        f = new Date(o),
        u, t;
    if (f = Date.parse(f) / 1e3, u = new Date, u = Date.parse(u) / 1e3, t = f - u, t < 0) {
        callHotDealHome();
        clearInterval(intervalFS);
        return
    }
    var e = Math.floor(t / 86400),
        n = Math.floor((t - e * 86400) / 3600),
        i = Math.floor((t - e * 86400 - n * 3600) / 60),
        r = Math.floor(t - e * 86400 - n * 3600 - i * 60);
    if (n < "10" && (n = "0" + n), i < "10" && (i = "0" + i), r < "10" && (r = "0" + r), parseInt(n) <= 0 && parseInt(i) <= 0 && parseInt(r) <= 0) {
        $(".flashsale-block").addClass("shutdown");
        setTimeout(function () {
            $(".flashsale-block").hide();
            $(".hotdeal").css("opacity", "0").removeClass("hide");
            setTimeout(function () {
                $(".flashsale-block").remove();
                $(".hotdeal").css("opacity", "1");
                clearInterval(intervalFS)
            }, 1e3)
        }, 1500);
        return
    }
    $("#hour").html(n);
    $("#minute").html(i);
    $("#second").html(r);
    $(".countdown-timer").removeClass("calling")
}

function slickFS() {
    $(".slider-flashsale .item").length > 12 && $(".slider-flashsale").data("mobile") != 1 && $(".slider-flashsale").slick({
        rows: 2,
        dots: !1,
        arrows: !0,
        infinite: !1,
        speed: 300,
        slidesToShow: 6,
        slidesToScroll: 6,
        prevArrow: '<button class="slide-arrow prev-arrow"><\/button>',
        nextArrow: '<button class="slide-arrow next-arrow"><\/button>'
    })
}

function callHotDealHome() {
    $(".flashsale-block").remove();
    typeof owlSliderPromo != "undefined" && (owlSliderPromo.trigger("destroy.owl.carousel"), setTimeout(function () {
        sliderPromoInitCarousel()
    }, 1e3));
    $(".hotdeal").removeClass("hide")
}

function GetCrmFS() {
    $(".flashsale-block .slider-flashsale .item").each(function () {
        var t = $(this).data("id"),
            n = $(this);
        $.ajax({
            url: "/HomeV2/GetCrmCountByProductId",
            data: {
                productId: t,
                beginfsdatetime: $(".flashsale-block .endtime").data("begin")
            },
            cache: !1,
            type: "POST",
            "async": !0,
            beforeSend: function () { },
            success: function (t) {
                n.find(".fs-contain").hide().html(t).fadeIn();
                let i = n.find(".fs-contain"),
                    r = i.find(".rq_count.fscount");
                r.hasClass("sold-out") && (r.removeClass("sold-out"), n.addClass("sold-out"), n.find(".item-img").append(i.find(".layer-sold-out")))
            },
            error: function (n) {
                console.log(n)
            }
        })
    })
}
var gl_getJsCmt = !1,
    gl_getJsCmtDmx = !1,
    gl_siteID = 1,
    comment_cdn = "https://www.thegioididong.com/commentnew",
    tgddc_urlroot = "//www.thegioididong.com/commentnew",
    comment_detailmobile = "commentMWG_21",
    uploadedFile = 0,
    gl_isGetScript, gl_rtCurStar, gl_sendRating, owlSliderPromo, intervalFS;
$(document).ready(function () {
    if ($(".rating-cmt-info-js").length != 0) {
        var n = parseInt($(".rating-cmt-info-js").attr("siteid"));
        n == 2 && (gl_siteID = 2, comment_cdn = "https://cdn.tgdd.vn/dienmay2015/comment", tgddc_urlroot = "//www.dienmayxanh.com/comment");
        LoadJsComment();
        $(window).scroll(function () { })
    }
});
var rooturl = null,
    gl_productID = null,
    gl_isFeedbackLoad = !1,
    gl_rt_currentPage = 0,
    gl_rtCurStar = 0,
    gl_rt_currentImg = !1,
    gl_rt_currentOrder = 1,
    gl_rt_siteID = 1,
    gl_rt_isRatingPage = 0,
    gl_rt_isMobile = null,
    rating_container = null,
    rating_block = null,
    rating_popup = null,
    productUrl = null;
$(document).ready(function () {
    console.log("rtPage_m_v2");
    $(".rating-product .close-cmt").click(function () {
        let t = $(".rating-product"),
            i = $(this),
            n = i.closest(".rating-product__block"),
            r = t.find(".rating-product__popup[data-index=" + n.data("index") + "]"),
            u = n.data("phone"),
            f = n.data("order-detail-id");
        var e = {
            customerPhone: u,
            orderDetailId: f
        };
        $.ajax({
            url: "/Home/HideInviteRatingProduct",
            data: e,
            method: "POST",
            beforeSend: function () { },
            success: function (i) {
                i.code == 200 && RemoveBlockPopup(t, n, r)
            },
            error: function () { }
        })
    })
});
gl_isGetScript = !1;
gl_rtCurStar = 0;
gl_sendRating = !1;

function initCarousel () {
    if (isMobile ? (InitCarousel(), ShowHideStickyButton()) : InitOwlCarousel(), SetAllContentAttr(), isMobile) $(".home-nav .iconnew-boxarrow").click(function () {
        $(".home-nav").toggleClass("active")
    }), $(window).scroll(function () {
        ShowHideStickyButton()
    });
    else {
        let t = $("section.main-container"),
            n = $(".sticky-sidebar");
        $(window).scroll(function () {
            t.isReachViewportTop() ? n.addClass("active") : n.removeClass("active")
        })
    }
    $(".option-sg a").click(function () {
        let t = $(this);
        if (t.hasClass("active")) return !1;
        let n = t.closest(".daily-sg"),
            f = n.find(".preloader"),
            e = t.data("is-recommend-tab"),
            r = t.data("campaign"),
            u = t.data("group"),
            i = n.find(".block-product__content" + (e == !0 ? "[data-is-recommend-tab=" + e + "]" : ":not([data-is-recommend-tab])") + (r != null ? "[data-campaign=" + r + "]" : ":not([data-campaign])") + (u != null ? "[data-group=" + u + "]" : ":not([data-group])"));
        if (i != null && i.length > 0) return n.find(".option-sg a").removeClass("active"), t.addClass("active"), n.find(".block-product__content").hide(), n.find(".listproduct").removeClass("tracking"), i.show(), i.find(".listproduct").addClass("tracking"), !1;
        let o = {
            campaignId: r,
            groupId: u
        };
        $.ajax({
            url: "/HomeV2/GetListProduct",
            data: o,
            method: "POST",
            beforeSend: function () {
                n.find(".option-sg a").removeClass("active");
                t.addClass("active");
                f.show()
            },
            success: function (f) {
                n.find(".block-product__content").hide();
                n.find(".listproduct").removeClass("tracking");
                n.find(".block-product").append(f);
                i = n.find(".block-product__content").last();
                r != null && i.attr("data-campaign", r);
                u != null && i.attr("data-group", u);
                n.find(".listproduct").last().addClass("tracking");
                let e = i.find(".item > a");
                if (e.length > 0) {
                    let n = "Home page - " + t.find("span").text().trim().replace("\n", "");
                    addListProductIntoImpression(e, n, 10)
                }
            },
            complete: function () {
                f.hide()
            }
        })
    });
    $(document).on("click", ".block-product__content .readmore-btn", function () {
        let n = $(this),
            r = n.closest(".block-product__content"),
            f = r.find(".readmore-btn-link"),
            t = r.find(".listproduct"),
            e = t.find(".item"),
            i = parseInt(n.attr("data-page-index")),
            u = n.data("page-size"),
            o = t.data("total");
        e.slice(i * u, (i + 1) * u).removeClass("hide");
        t.find(".item:not(.hide)").length == o ? (n.remove(), f.removeClass("hide")) : n.attr("data-page-index", i + 1)
    });
    GetPopupRemindGiftVoucher()
};
$.fn.isInViewport = function () {
    let n = $(this).offset().top,
        i = n + $(this).outerHeight(),
        t = $(window).scrollTop(),
        r = t + $(window).height();
    return i > t && n < r
};
$.fn.isReachViewportTop = function (n = 0) {
    let t = $(this).offset().top,
        r = t + $(this).outerHeight(),
        i = $(window).scrollTop();
    return i < r - n && i >= t - n
};

function Realdy() {
    !1 && $(".slider-flashsale .item").length <= 0 ? callHotDealHome() : (intervalFS = setInterval(function () {
        flashsaleCountdown()
    }, 1e3), slickFS(), GetCrmFS());
    $(".listing-timeline a").click(function (n) {
        n.preventDefault();
        var t = $(this);
        t.hasClass("active") || ($(".listing-timeline a").removeClass("active"), t.addClass("active"), $.ajax({
            url: "/HomeV2/GetProductListFlashsale",
            data: {
                productIds: t.data("productlist"),
                isHappening: t.data("ishappening"),
                beginfsdatetime: $(".flashsale-block .endtime").data("begin")
            },
            cache: !1,
            type: "POST",
            beforeSend: function () {
                $(".flashsale-block .stage-two").show()
            },
            success: function (n) {
                if ($(".slider-flashsale").data("mobile") != 1) try {
                    $(".slider-flashsale").slick("unslick")
                } catch (i) {
                    console.log("call slick error!")
                }
                $(".listproduct.slider-flashsale").html(n);
                var t = $(n).not("text").length;
                $(".listproduct.slider-flashsale").removeClass("two-line").removeClass("has-scroll");
                $(".listproduct.slider-flashsale").removeClass("has-slick");
                $(".listproduct.slider-flashsale").removeClass("flow-row");
                t > 3 && ($(".listproduct.slider-flashsale").addClass("two-line"), t > 6 && $(".listproduct.slider-flashsale").addClass("has-scroll"));
                t > 12 && $(".listproduct.slider-flashsale").addClass("has-slick");
                t <= 4 && $(".listproduct.slider-flashsale").addClass("flow-row");
                slickFS();
                $(".flashsale-block .stage-two").hide()
            },
            error: function (n) {
                console.log(n)
            }
        }))
    })
}