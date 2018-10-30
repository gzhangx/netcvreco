import request from 'superagent';

export default {
  cmdReq: url=>{
      return request.get(`/${url}`).send().then(r=> {
          console.log('request returns');
          console.log(r);
      });
  }
};